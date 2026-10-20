namespace Capastrum.JsonRpc;

/// <summary>
/// Standard JSON-RPC 2.0 error codes.
/// </summary>
public static class JsonRpcErrorCode
{
    /// <summary>Invalid JSON was received.</summary>
    public const int ParseError = -32700;

    /// <summary>The JSON was valid but not a valid request.</summary>
    public const int InvalidRequest = -32600;

    /// <summary>The method does not exist or is not available.</summary>
    public const int MethodNotFound = -32601;

    /// <summary>The method parameters are invalid.</summary>
    public const int InvalidParams = -32602;

    /// <summary>An internal error occurred.</summary>
    public const int InternalError = -32603;
}
