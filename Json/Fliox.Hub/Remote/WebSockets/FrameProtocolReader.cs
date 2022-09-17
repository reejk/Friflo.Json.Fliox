﻿// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// ReSharper disable SuggestBaseTypeForParameter
namespace Friflo.Json.Fliox.Hub.Remote.WebSockets
{
    public sealed class FrameProtocolReader
    {
        public              bool                    EndOfMessage            { get; private set; }
        public              int                     ByteCount               => dataBufferPos;
        public              WebSocketMessageType    MessageType             { get; private set; }
        public              WebSocketCloseStatus?   CloseStatus             { get; private set; }
        public              WebSocketState          SocketState             { get; private set; }
        public              string                  CloseStatusDescription  { get; private set; }
        /// <summary> store the bytes read from the socket.
        /// <see cref="bufferPos"/> is its read position and <see cref="bufferLen"/> the count of bytes read from socket</summary>
        private  readonly   byte[]                  buffer;
        private             int                     bufferPos;
        private             int                     bufferLen;
        private             long                    processedByteCount;
        /// <summary> general <see cref="parseState"/> and its sub states <see cref="payloadLenPos"/> and <see cref="maskingKeyPos"/> </summary>
        private             Parse                   parseState;
        private             bool                    fin;
        private             Opcode                  opcode;
        private             int                     payloadLenBytes;
        private             int                     payloadLenPos;
        private             int                     maskingKeyPos;
        /// <summary>write position of given <see cref="dataBuffer"/> </summary>
        private             int                     dataBufferPos;
        private             byte[]                  dataBuffer;
        /// <summary>[RFC 6455: The WebSocket Protocol - Control Frames] https://www.rfc-editor.org/rfc/rfc6455#section-5.5.1 </summary>
        private readonly    byte[]                  controlFrameBuffer;
        private             int                     controlFrameBufferPos;
        /// <summary> <see cref="payloadPos"/> read position payload. Increments up to <see cref="payloadLen"/> </summary>
        private             long                    payloadPos;
        private             long                    payloadLen;
        // --- Base Framing Protocol headers
        private             bool                    mask;
        private readonly    byte[]                  maskingKey = new byte[4];
        
        public FrameProtocolReader(int bufferSize = 4096) {
            buffer              = new byte[bufferSize];
            SocketState         = WebSocketState.Open;
            controlFrameBuffer  = new byte[125];
        }

        public async Task<WebSocketState> ReadFrame(Stream stream, byte[] dataBuffer, CancellationToken cancellationToken)
        {
            if (SocketState != WebSocketState.Open) throw new InvalidOperationException("reader already closed");
            this.dataBuffer = dataBuffer;
            dataBufferPos   = 0;
            while (true) {
                // process unprocessed bytes in buffer from previous call
                var  startPos       = bufferPos;
                bool frameEnd       = ProcessFrame();
                processedByteCount += bufferPos - startPos;
                
                if (frameEnd) {
                    // var debugStr = Encoding.UTF8.GetString(dataBuffer.Array, 0, ByteCount);
                    return SocketState;
                }
                bufferPos = 0;
                bufferLen = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false);
                if (bufferLen < 1) {
                    SocketState             = WebSocketState.Closed;
                    CloseStatus             = WebSocketCloseStatus.EndpointUnavailable;
                    CloseStatusDescription  = "stream closed";
                    MessageType             = WebSocketMessageType.Close;
                    EndOfMessage            = true;
                    return SocketState;
                }
            }
        }
        
        /// general state of state machine 
        private enum Parse {
            Opcode,
            PayloadLen,
            Masking,
            Payload,
        }

        /// <summary>
        /// return true:  if reading payload is complete or the given <see cref="dataBuffer"/> is filled
        /// return false: if more frame bytes need to be read
        /// </summary>
        private bool ProcessFrame ()
        {
            // performance: use locals enable CPU using these values from stack
            var buf     = buffer;
            var len     = bufferLen;

            while (bufferPos < len) {
                byte b;
                switch (parseState) {
                    case Parse.Opcode:
                        b               =  buf[bufferPos++];
                        fin             =          (b & (int)FrameFlags.Fin) != 0;
                        opcode          = (Opcode) (b & (int)FrameFlags.Opcode);
                        MessageType     = GetMessageType(opcode);
                        payloadLenPos   = -1;
                        parseState      = Parse.PayloadLen;
                        break;
                    
                    case Parse.PayloadLen:
                        b               =  buf[bufferPos++];
                        if (payloadLenPos == -1) {
                            mask            = (b & (int)LenFlags.Mask) != 0; 
                            payloadLen      = b & 0x7f;
                            payloadLenPos   = 0;
                            if (payloadLen == 126) {
                                payloadLen      = 0;
                                payloadLenBytes = 2;
                                break;
                            }
                            if (payloadLen == 127) {
                                payloadLen      = 0;
                                payloadLenBytes = 8;
                                break;
                            }
                        } else {
                            // payload length uses network byte order (big endian). E.g 0x0102 -> byte[] { 01, 02 }
                            payloadLen = (payloadLen << 8) | b;
                            if (++payloadLenPos < payloadLenBytes)
                                break;
                        }
                        maskingKeyPos   = 0;
                        if (mask) {
                            parseState  = Parse.Masking;
                            break;
                        }
                        if (TransitionPayload())
                            break;
                        return true; // empty payload
                    
                    case Parse.Masking:
                        b =  buf[bufferPos++];
                        maskingKey[maskingKeyPos++] = b;
                        if (maskingKeyPos < 4) {
                            break;
                        }
                        if (TransitionPayload())
                            break;
                        return true; // empty payload
                    
                    case Parse.Payload:
                        var dataBufferStart = dataBufferPos;
                        var payloadResult   = ReadPayload(buf, len);

                        if (opcode == Opcode.ConnectionClose) {
                            UpdateControlFrameBuffer(dataBufferStart);
                        }
                        return payloadResult;
                }
            }
            return false;
        }
        
        private bool TransitionPayload() {
            if (payloadLen > 0) {
                payloadPos              = 0;
                parseState              = Parse.Payload;
                controlFrameBufferPos   = 0;
                return true;
            }
            parseState      = Parse.Opcode;
            EndOfMessage    = fin;
            return false;
        }
        
        /// <summary>
        /// return true:  if reading payload is complete or the given <see cref="dataBuffer"/> is filled.
        /// return false: if more payload bytes need to be read
        /// </summary>
        private bool ReadPayload(byte[] buf, int len)
        {
            // performance: use locals enable CPU using these values from stack
            var localDataBuffer = dataBuffer;
            var localPayloadPos = payloadPos;
            var dataBufferLen   = dataBuffer.Length;
            
            var payloadDif      = payloadLen    - payloadPos; 
            var dataBufferDif   = dataBufferLen - dataBufferPos;
            var bufferDif       = len           - bufferPos;
            
            var minIterations   = (int)(payloadDif    <= dataBufferDif ? payloadDif    : dataBufferDif);
            minIterations       =       minIterations <= bufferDif     ? minIterations : bufferDif;
            
            var pos     = bufferPos;
            var dataPos = dataBufferPos;
            
            if (mask) {
                var localMaskingKey = maskingKey;
                for (int n = 0; n < minIterations; n++) {
                    var b = buf[pos + n];
                    localDataBuffer[dataPos + n] = (byte)(b ^ localMaskingKey[(localPayloadPos + n) % 4]);
                }
            } else {
                Buffer.BlockCopy(buf, pos, localDataBuffer, dataPos, minIterations);
            }
            bufferPos       += minIterations;
            dataBufferPos   += minIterations;
            payloadPos      += minIterations;

            if (payloadPos == payloadLen) {
                parseState      = Parse.Opcode;
                EndOfMessage    = fin;
                return true;
            }
            if (dataBufferPos == dataBufferLen) {
                EndOfMessage    = false;
                return true;
            }
            return false;
        }
        
        private void UpdateControlFrameBuffer(int dataBufferStart) {
            var bytesAdded = dataBufferPos - dataBufferStart;
            Buffer.BlockCopy(dataBuffer, dataBufferStart, controlFrameBuffer, controlFrameBufferPos, bytesAdded);
            controlFrameBufferPos += bytesAdded;
            if (!EndOfMessage)
                return;
            // [RFC 6455: The WebSocket Protocol - Close] https://www.rfc-editor.org/rfc/rfc6455#section-5.5.1
            SocketState             = WebSocketState.CloseReceived;
            if (controlFrameBufferPos >= 2) {
                CloseStatus             = (WebSocketCloseStatus)(controlFrameBuffer[0] << 8 | controlFrameBuffer[1]);
                CloseStatusDescription  = Encoding.UTF8.GetString(controlFrameBuffer, 2, controlFrameBufferPos - 2);
            } else {
                CloseStatus             = null;
                CloseStatusDescription  = "";
            }
        }
        
        private static WebSocketMessageType GetMessageType(Opcode opcode) {
            switch(opcode) {
                case Opcode.TextFrame:      return WebSocketMessageType.Text;
                case Opcode.BinaryFrame:    return WebSocketMessageType.Binary;
                default:                    return WebSocketMessageType.Close;
            }
        }
    }
}