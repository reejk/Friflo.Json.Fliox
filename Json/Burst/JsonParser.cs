// Copyright (c) Ullrich Praetz. All rights reserved.
// See LICENSE file in the project root for full license information.
using System;
using Friflo.Json.Burst.Utils;

#if JSON_BURST
	using Str32 = Unity.Collections.FixedString32;
	using Str128 = Unity.Collections.FixedString128;
#else
	using Str32 = System.String;
	using Str128 = System.String;
#endif

namespace Friflo.Json.Burst
{
	public struct SkipInfo {
		public int arrays;
		public int booleans;
		public int floats;
		public int integers;
		public int nulls;
		public int objects;
		public int strings;

		public int Sum => arrays + booleans + floats + integers + nulls + objects + strings;

		public bool IsEqual(SkipInfo si) {
			return arrays == si.arrays && booleans == si.booleans && floats == si.floats && integers == si.integers &&
			       nulls == si.nulls && objects == si.objects && strings == si.strings;
		}

		public Str128 ToStr128() {
			return $"[ arrays:{arrays} booleans: {booleans} floats: {floats} integers: {integers} nulls: {nulls} objects: {objects} strings: {strings} ]";
		}
		
		public override string ToString() {
			return ToStr128().ToString();
		}
	}
	
	public partial struct JsonParser : IDisposable
	{
		private 			int					pos;
		private 			ByteList			buf;
		private 			int					bufEnd;
		private				int					stateLevel;
		private				int					startPos;
	//	public				JsonEvent			lastEvent { get ; private set; } bad idea, lastEvent is only relevant for the api user

		private				int					preErrorState;
		private 			ValueArray<int>		state;
		private 			ValueArray<int>		pathPos;  	// used for current path
		private 			ValueArray<int>		arrIndex;  	// used for current path
		public				ErrorCx				error;
	
		public				bool				boolValue;
		public				Bytes				key;
		public				Bytes				value;
		private				Bytes				path;		// used for current path storing the path segments names
		private				Bytes				errVal;		// used for conversion of an additional value in error message creation
		private				Bytes				getPathBuf; // MUST be used only in GetPath()
		
		private				ValueFormat			format;
		private				ValueParser			valueParser;

		private				Str32				@true;
		private				Str32				@false;
		private				Str32				@null;
		private				Str32				emptyArray;

		public				bool				isFloat;
		public				SkipInfo			skipInfo;
		
	
		private const int 	ExpectMember =			0;
		private const int 	ExpectMemberFirst =		1;
	
		private const int 	ExpectElement =			2;
		private const int 	ExpectElementFirst =	3;
	
		private const int 	ExpectRoot =			4; // only set at state[0]
		private const int 	ExpectEof =				5; // only set at state[0]
		
		private const int 	JsonError =				6;
		private const int 	Finished =				7; // only set at state[0]
	
		public int			GetLevel()	{	return stateLevel;		}

		// ---------------------- error message creation - begin
		public void Error (Str32 module, ref Str128 msg, ref Bytes value) {
			preErrorState = state[stateLevel]; 
			state[stateLevel] = JsonError;
			if (error.ErrSet)
				throw new InvalidOperationException("JSON Error already set"); // If setting error again the relevant previous error would be overwritten.
			
			int position = pos - startPos;
			// Note 1:	Creating error messages complete avoid creating string on the heap to ensure no allocation
			//			in case of errors. Using string interpolation like $"...{}..." create objects on the heap
			// Note 2:  Even cascaded string interpolation does not work in Unity Burst. So using appenders make sense too.

			// Pseudo format: $"{module} error - {msg}{value} path: {path} at position: {position}";
			ref Bytes errMsg = ref error.msg;
			errMsg.Clear();
			errMsg.AppendStr32(ref module);
			errMsg.AppendStr32(" error - ");
			errMsg.AppendStr128(ref msg);
			errMsg.AppendBytes(ref value);
			errMsg.AppendStr32(" path: '");
			AppendPath(ref errMsg);
			errMsg.AppendStr32("' at position: ");
			format.AppendInt(ref errMsg, position);
			error.Error(pos);
		}
		
		public void Error(Str32 module, Str128 msg) {
			errVal.Clear();
			Error(module, ref msg, ref errVal);
		}
		
		private JsonEvent SetError (Str128 msg) {
			errVal.Clear();
			Error("JsonParser", ref msg, ref errVal);
			return JsonEvent.Error;
		}
		
		private JsonEvent SetErrorChar (Str128 msg, char c) {
			errVal.Clear();
			errVal.AppendChar(c);
			Error("JsonParser", ref msg, ref errVal);
			return JsonEvent.Error;
		}
		
		private bool SetErrorValue (Str128 msg, ref Bytes value) {
			Error("JsonParser", ref msg, ref value);
			return false;
		}

		private bool SetErrorFalse (Str128 msg)
		{
			errVal.Clear();
			Error("JsonParser", ref msg, ref errVal);
			return false;
		}
		
		private bool SetErrorEvent (Str128 msg, JsonEvent ev)
		{
			errVal.Clear();
			JsonEventUtils.AppendEvent(ev, ref errVal);
			Error("JsonParser", ref msg, ref errVal);
			return false;
		}
		// ---------------------- error message creation - end

		public string GetPath() {
			getPathBuf.Clear();
			AppendPath(ref getPathBuf);
			return getPathBuf.ToString();
		}
		
		public override string ToString() {
			return $"{{ path: \"{GetPath()}\", pos: {pos} }}";
		}
		
		public void AppendPath(ref Bytes str) {
			int initialEnd = str.end;
			int lastPos = 0;
			int level = stateLevel;
			bool errored = state[level] == JsonError;
			if (errored)
				level++;
			for (int n = 1; n <= level; n++) {
				int curState = state[n];
				int index = n;
				if (errored && n == level) {
					curState = preErrorState;
					index = n - 1;
				}
				switch (curState) {
					case ExpectMember:
						if (index > 1)
							str.AppendChar('.');
	                    str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos[index]);
	                    break;
					case ExpectMemberFirst:
						str.AppendArray(ref path.buffer, lastPos, lastPos= pathPos[index]);
						break;
					case ExpectElement:
					case ExpectElementFirst:
						if (arrIndex[index] != -1)
						{
							str.AppendChar('[');
							format.AppendInt(ref str, arrIndex[index]);
							str.AppendChar(']');
						}
						else
							str.AppendStr32(ref emptyArray);
						break;
				}
			}
			if (initialEnd == str.end)
				str.AppendStr32("(root)");
		}

		private void InitContainers() {
			if (state.IsCreated())
				return;
			state =	 new ValueArray<int>(32);
			pathPos = new ValueArray<int>(32);
			arrIndex = new ValueArray<int>(32);
			error.InitErrorCx(128);
			key.InitBytes(32);
			path.InitBytes(32);
			errVal.InitBytes(32);
			getPathBuf.InitBytes(32);
			value.InitBytes(32);
			format.InitTokenFormat();
			@true =			"true";
			@false =		"false";
			@null =			"null";
			emptyArray =	"[]";
			valueParser.InitValueParser();
		}

		public void Dispose() {
			valueParser.Dispose();
			format.Dispose();
			value.Dispose();
			getPathBuf.Dispose();
			errVal.Dispose();
			path.Dispose();
			key.Dispose();
			error.Dispose();
			if (arrIndex.IsCreated())	arrIndex.Dispose();
			if (pathPos.IsCreated())	pathPos.Dispose();
			if (state.IsCreated())		state.Dispose();
		}
		
		public void InitParser(Bytes bytes) {
			InitParser (bytes.buffer, bytes.Start, bytes.Len);
		}

		public void InitParser(ByteList bytes, int offset, int len) {
			InitContainers();
			stateLevel = 0;
			state[0] = ExpectRoot;

			this.pos = offset;
			this.startPos = offset;
			this.buf = bytes;
			this.bufEnd = offset + len;
			skipInfo = default(SkipInfo);
			error.Clear();
		}

		/* public JsonEvent NextEvent() {
			JsonEvent ev = nextEvent();
			lastEvent = ev;
			return ev;
		} */

		public JsonEvent NextEvent()
		{
			int c = ReadWhiteSpace();
			int curState = state[stateLevel];
			switch (curState)
			{
    		case ExpectMember:
    		case ExpectMemberFirst:
				switch (c)
				{
					case ',':
						if (curState == ExpectMemberFirst)
							return SetError ("unexpected member separator");
						c = ReadWhiteSpace();
						if (c != '"')
							return SetErrorChar ("expect key. Found: ", (char)c);
						break;
		            case '}':
		            	stateLevel--;
		            	return JsonEvent.ObjectEnd;
		            case  -1:
		            	return SetError("unexpected EOF - expect key");
		            case '"':
		            	if (curState == ExpectMember)
		            		return SetError ("expect member separator");
		            	break;
		            default:
		            	return SetErrorChar("unexpected character - expect key. Found: ", (char)c);
				}
	        	// case: c == '"'
				state[stateLevel] = ExpectMember;
	        	if (!ReadString(ref key))
	        		return JsonEvent.Error;
	        	// update current path
	        	path.SetEnd(pathPos[stateLevel-1]);  // "Clear"
	        	path.AppendBytes(ref key);
	        	pathPos[stateLevel] = path.End;
	        	//
	        	c = ReadWhiteSpace();
	       		if (c != ':')
	       			return SetErrorChar ("Expected ':' after key. Found: ", (char)c);
	    		c = ReadWhiteSpace();
	            break;
            
    		case ExpectElement:
    		case ExpectElementFirst:
				arrIndex[stateLevel]++;
				if (c == ']')
				{
					stateLevel--;
					return JsonEvent.ArrayEnd;
				}
    			if (curState == ExpectElement)
    			{
	    			if (c != ',')
	    				return SetErrorChar("expected array separator ','. Found: ", (char)c);
    				c = ReadWhiteSpace();
    			}
    			else
    				state[stateLevel] = ExpectElement;
    			break;
    		
			case ExpectRoot:
				state[0] = ExpectEof;
        		switch (c)
        		{
					case '{':
						pathPos[stateLevel+1] = pathPos[stateLevel];
	            		state[++stateLevel] = ExpectMemberFirst;
	            		return JsonEvent.ObjectStart;
					case '[':
						pathPos[stateLevel+1] = pathPos[stateLevel];
	            		state[++stateLevel] = ExpectElementFirst;
						arrIndex[stateLevel] = -1;
	            		return JsonEvent.ArrayStart;
					case -1:
						return SetError("unexpected EOF on root");
					// default: read following bytes as value  
        		}
                break;
			
			case ExpectEof:
				if (c == -1) {
					state[0] = Finished;
					return JsonEvent.EOF;
				}
				return SetError("Expected EOF");
			
            case Finished:
	            return SetError("Parsing already finished");
            
			case JsonError:
				return JsonEvent.Error;
			}
        
			// ---- read value of key/value pairs or array items ---
			switch (c)
			{
				case '"':
            		if (ReadString(ref value))
            			return JsonEvent.ValueString;
            		return JsonEvent.Error;
				case '{':
					pathPos[stateLevel+1] = pathPos[stateLevel];
            		state[++stateLevel] = ExpectMemberFirst;
            		return JsonEvent.ObjectStart;
				case '[':
					pathPos[stateLevel+1] = pathPos[stateLevel];
            		state[++stateLevel] = ExpectElementFirst;
					arrIndex[stateLevel] = -1;
            		return JsonEvent.ArrayStart;
				case '0':	case '1':	case '2':	case '3':	case '4':
				case '5':	case '6':	case '7':	case '8':	case '9':
				case '-':	case '+':	case '.':
            		if (ReadNumber())
            			return JsonEvent.ValueNumber;
            		return JsonEvent.Error;
				case 't':
            		if (!ReadKeyword(ref @true ))
            			return JsonEvent.Error;
            		boolValue= true;
            		return JsonEvent.ValueBool;
				case 'f':
            		if (!ReadKeyword(ref @false))
            			return JsonEvent.Error;
            		boolValue= false;
            		return JsonEvent.ValueBool;
				case 'n':
            		if (!ReadKeyword(ref @null))
            			return JsonEvent.Error;
            		return JsonEvent.ValueNull;
				case  -1:
            		return SetError("unexpected EOF while reading value");
				default:
	        		return SetErrorChar("unexpected character while reading value. Found: ", (char)c);
			}
			// unreachable
		}

		private int ReadWhiteSpace()
		{
			// using locals improved performance
			ref var b = ref buf.array;
			int p = pos;
			int end = bufEnd;
			for (; p < end; )
			{
				int c = b[p++];
				if (c > ' ') {
					pos = p;
            		return c;
				}
				if (c != ' '	&&
            		c != '\t'	&&
            		c != '\n'	&&
            		c != '\r') {
					pos = p;
            		return c;
				}
			}
			pos = p;
			return -1;
		}
	
		private bool ReadNumber ()
		{
			isFloat = false;
			int start = pos - 1;
			for (; pos < bufEnd; pos++)
			{
				int c = buf.array[pos];
				switch (c)
				{
				case '0':	case '1':	case '2':	case '3':	case '4':
				case '5':	case '6':	case '7':	case '8':	case '9':
				case '-':	case '+':
					continue;
				case '.':	case 'e':	case 'E':
					isFloat = true;
            		continue;
				}
				switch (c) {
					case ',': case '}': case ']':
					case ' ': case '\r': case '\n': case '\t':
						value.Clear();
						value.AppendArray(ref buf, start, pos);
						return true;
				}
				SetErrorChar("unexpected character while reading number. Found : ", (char)c);
				return false;
			}
			if (state[stateLevel] != ExpectEof) 
				return SetErrorFalse("unexpected EOF while reading number");
			value.Clear();
			value.AppendArray(ref buf, start, pos);
			return true;
		}
	
		private bool ReadString(ref Bytes token)
		{
			// using locals improved performance
			ref var b = ref buf.array;
			int p = pos;
			int end = bufEnd;
			token.Clear();
			int start = p;
			for (; p < end; p++)
			{
				int c = b[p];
				if (c == '\"')
				{
            		token.AppendArray(ref buf, start, p++);
                    pos = p;
					return true;
				}
				if (c == '\r' || c == '\n')
					return SetErrorFalse("unexpected line feed while reading string");
				if (c == '\\')
				{
            		token.AppendArray(ref buf, start, p);
            		if (++p >= end)
            			break;
            		c = b[p];
            		switch (c)
            		{
					case '"':	token.AppendChar('"');	break;
					case '\\':	token.AppendChar('\\');	break;
					case '/':	token.AppendChar('/');	break;
					case 'b':	token.AppendChar('\b');	break;
					case 'f':	token.AppendChar('\f');	break;
					case 'r':	token.AppendChar('\r');	break;
					case 'n':	token.AppendChar('\n');	break;
					case 't':	token.AppendChar('\t');	break;                	
					case 'u':
						pos = p;
						if (!ReadUnicode(ref token))
							return false;
						p = pos;
						break;
            		}
            		start = p + 1;
				}
			}
			pos = p;
			return SetErrorFalse("unexpected EOF while reading string");
		}
	
		private bool ReadUnicode (ref Bytes tokenBuffer) {
			ref Bytes token = ref tokenBuffer;
			pos += 4;
			if (pos >= bufEnd)
				return SetErrorFalse("Expect 4 hex digits after '\\u' in value");

			int d1 = Digit2Int(buf.array[pos - 3]);
			int d2 = Digit2Int(buf.array[pos - 2]);
			int d3 = Digit2Int(buf.array[pos - 1]);
			int d4 = Digit2Int(buf.array[pos - 0]);
			if (d1 == -1 || d2 == -1 || d3 == -1 || d4 == -1)
				return SetErrorFalse("Invalid hex digits after '\\u' in value");

			int uni = d1 << 12 | d2 << 8 | d3 << 4 | d4;
		
			// UTF-8 Encoding
			tokenBuffer.EnsureCapacity(4);
			ref var str = ref token.buffer.array;
			int i = token.End;
			if (uni < 0x80)
			{
				str[i] =	(byte)uni;
				token.SetEnd(i + 1);
				return true;
			}
			if (uni < 0x800)
			{
				str[i]   =	(byte)(m_11oooooo | (uni >> 6));
				str[i+1] =	(byte)(m_1ooooooo | (uni 		 & m_oo111111));
				token.SetEnd(i + 2);
				return true;
			}
			if (uni < 0x10000)
			{
				str[i]   =	(byte)(m_111ooooo |  (uni >> 12));
				str[i+1] =	(byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
				str[i+2] =	(byte)(m_1ooooooo |  (uni        & m_oo111111));
				token.SetEnd(i + 3);
				return true;
			}
			str[i]   =		(byte)(m_1111oooo |  (uni >> 18));
			str[i+1] =		(byte)(m_1ooooooo | ((uni >> 12) & m_oo111111));
			str[i+2] =		(byte)(m_1ooooooo | ((uni >> 6)  & m_oo111111));
			str[i+3] =		(byte)(m_1ooooooo |  (uni        & m_oo111111));
			token.SetEnd(i + 4);
			return true;
		}
	
		private static readonly int 	m_1ooooooo = 0x80;
		private static readonly int 	m_11oooooo = 0xc0;
		private static readonly int 	m_111ooooo = 0xe0;
		private static readonly int 	m_1111oooo = 0xf0;
	
		private static readonly int 	m_oo111111 = 0x3f;
	
		private static int Digit2Int (int c)
		{
			if ('0' <= c && c <= '9')
				return c - '0';
			if ('a' <= c && c <= 'f')
				return c - 'a' + 10;
			if ('A' <= c && c <= 'F')
				return c - 'A' + 10;
			return -1;
		}
	
		private bool ReadKeyword (ref Str32 keyword)
		{
			int start = pos - 1;
			ref var b = ref buf.array;
			for (; pos < bufEnd; pos++)
			{
				int c = b[pos];
				if ('a' <= c && c <= 'z')
					continue;
				break;
			}
			int len = pos - start;
			int keyLen = keyword.Length;
			if (len != keyLen) {
				value.Clear();
				value.AppendArray(ref buf, start, pos);
				return SetErrorValue("invalid value: ", ref value);
			}

			for (int n = 1; n < len; n++)
			{
				if (keyword[n] != b[start + n]) {
					value.Clear();
					value.AppendArray(ref buf, start, pos);
					return SetErrorValue("invalid value: ", ref value);
				}
			}
			return true;
		}

		public bool SkipTree()
		{
	        int curState = state[stateLevel];
	        switch (curState)
	        {
	        case ExpectMember:
	        case ExpectMemberFirst:
	        	return SkipObject();
	        case ExpectElement:
	        case ExpectElementFirst:
	        	return SkipArray();
	        case ExpectRoot:
		        JsonEvent ev = NextEvent();
		        return SkipEvent(ev);
	        default:
		        // dont set error. It would overwrite a previous error (parser state did not change)
		        return false;
	        }
		}
		
		private bool SkipObject()
		{
			skipInfo.objects++;
			while (true)
			{
				JsonEvent ev = NextEvent();
				switch (ev)
				{
				case JsonEvent. ValueString:
					skipInfo.strings++;
					break;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					break;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					break;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					break;
				case JsonEvent. ObjectStart:
					if (!SkipObject())
						return false;
					break;
				case JsonEvent. ArrayStart:
					if(!SkipArray())
						return false;
					break;
				case JsonEvent. ObjectEnd:
					return true;
				case JsonEvent. Error:
					return false;
				default:
					return SetErrorEvent("unexpected state: ", ev);
				}
			}
		}
		
		private bool SkipArray()
		{
			skipInfo.arrays++;
			while (true)
			{
				JsonEvent ev = NextEvent();
				switch (ev)
				{
				case JsonEvent. ValueString:
					skipInfo.strings++;
					break;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					break;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					break;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					break;
				case JsonEvent. ObjectStart:
					if (!SkipObject())
						return false;
					break;
				case JsonEvent. ArrayStart:
					if(!SkipArray())
						return false;
					break;
				case JsonEvent. ArrayEnd:
					return true;
				case JsonEvent. Error:
					return false;
				default:
					return SetErrorEvent("unexpected state: ", ev);
				}
			}
		}
		
		public bool SkipEvent (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ArrayStart:
				case JsonEvent.ObjectStart:
					return SkipTree();
				case JsonEvent.ArrayEnd:
				case JsonEvent.ObjectEnd:
					return false;
				case JsonEvent. ValueString:
					skipInfo.strings++;
					return true;
				case JsonEvent. ValueNumber:
					if (isFloat)
						skipInfo.floats++;
					else
						skipInfo.integers++;
					return true;
				case JsonEvent. ValueBool:
					skipInfo.booleans++;
					return true;
				case JsonEvent. ValueNull:
					skipInfo.nulls++;
					return true;
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true; // unreachable
		}
		
		public bool ContinueObject (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ArrayEnd:
					return SetErrorFalse("Unexpected JsonEvent.ArrayEnd in ContinueObject");
				case JsonEvent.ObjectEnd:
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true;
		}
		
		public bool ContinueArray (JsonEvent ev) {
			switch (ev) {
				case JsonEvent.ObjectEnd:
					return SetErrorFalse("Unexpected JsonEvent.ObjectEnd in ContinueArray");
				case JsonEvent.ArrayEnd:
				case JsonEvent.Error:
				case JsonEvent.EOF:
					return false;
			}
			return true;
		}

		public double ValueAsDoubleStd(out bool success) {
			double result = valueParser.ParseDoubleStd(ref value, ref errVal, out success);
			if (!success)
				SetErrorValue("", ref errVal);
			return result;
		}
		
		public double ValueAsDouble(out bool success) {
			double result = valueParser.ParseDouble(ref value, ref errVal, out success);
			if (!success) 
				SetErrorValue("", ref errVal);
			return result;
		}
		
		public float ValueAsFloat(out bool success) {
			double result = valueParser.ParseDouble(ref value, ref errVal, out success);
			if (!success) 
				SetErrorValue("", ref errVal);
			if (result < float.MinValue) {
				SetErrorValue("float is less than float.MinValue. ", ref value);
				return 0;
			}
			if (result > float.MaxValue) {
				SetErrorValue("float is greater than float.MaxValue. ", ref value);
				return 0;
			}
			return (float)result;
		}
		
		public long ValueAsLong(out bool success) {
			long result = valueParser.ParseLong(ref value, ref errVal, out success);
			if ( !success)
				SetErrorValue("", ref errVal);
			return result;
		}
		
		public int ValueAsInt(out bool success) {
			int result = valueParser.ParseInt(ref value, ref errVal, out success);
			if (!success)
				SetErrorValue("", ref errVal);
			return result;
		}

	}
}