namespace Friflo.Json.Burst
{
    public enum JsonEvent
    {
        ValueString,	// key is set, if inside an object
        ValueNumber,	// key is set, if inside an object
        ValueBool,		// true, false. key is set, if inside an object
        ValueNull,		// key is set, if inside an object
	
        ObjectStart,	// key is set, if inside an object
        ObjectEnd,
	
        ArrayStart,		// key is set, if inside an object
        ArrayEnd,
	
        EOF,
        Error,
    }
}