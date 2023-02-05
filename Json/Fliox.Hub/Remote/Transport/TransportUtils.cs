// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

// ReSharper disable once CheckNamespace
namespace Friflo.Json.Fliox.Hub.Remote
{
    internal static class TransportUtils
    {
        internal static IPEndPoint ParseEndpoint(string endpoint) {
            if (endpoint == null) {
                return null;
            }
            int addressLength   = endpoint.Length;  // If there's no port then send the entire string to the address parser
            int lastColonPos    = endpoint.LastIndexOf(':');
            // Look to see if this is an IPv6 address with a port.
            if (lastColonPos > 0) {
                if (endpoint[lastColonPos - 1] == ']') {
                    addressLength = lastColonPos;
                }
                // Look to see if this is IPv4 with a port (IPv6 will have another colon)
                else if (endpoint.Substring(0, lastColonPos).LastIndexOf(':') == -1) {
                    addressLength = lastColonPos;
                }
            }
            var address = endpoint.Substring(0, addressLength);
            if (address == "localhost") address = "127.0.0.1";
            if (!IPAddress.TryParse(address, out IPAddress ipAddress)) {
                return null;
            }
            if (addressLength == endpoint.Length) {
                return null;
            }
            if (!int.TryParse(endpoint.AsSpan(addressLength + 1), NumberStyles.None, CultureInfo.InvariantCulture, out int port)) {
                return null;
            }
            if (port > IPEndPoint.MaxPort) {
                return null;
            }
            return new IPEndPoint(ipAddress, port);
        }
        
        public static string Truncate(this string value) {
            const int maxLength = 120;
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength); 
        }
        
        public static void LogMessage(IHubLogger logger, string name, object endpoint, in JsonValue message) {
            logger.Log(HubLog.Info, $"{name}{endpoint,20} {message.AsString().Truncate()}");
        }
        
        public static string GetExceptionMessage(string location, IPEndPoint endpoint, Exception e) {
            if (e is SocketException socketException) {
                return $"{location} {e.GetType().Name} {e.Message} ErrorCode: {socketException.ErrorCode}, HResult: 0x{e.HResult:X}, endpoint: {endpoint}";
            }
            return $"{location} {e.GetType().Name}: {e.Message}, endpoint: {endpoint}";
        }
    }
    
    internal readonly struct UdpMeta
    {
        internal readonly   IPEndPoint  remoteEndPoint;

        public   override   string      ToString() => remoteEndPoint.ToString();

        internal UdpMeta (IPEndPoint remoteEndPoint) {
            this.remoteEndPoint = remoteEndPoint ?? throw new ArgumentNullException(nameof(remoteEndPoint));
        }
    }
}