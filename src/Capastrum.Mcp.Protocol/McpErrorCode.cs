namespace Capastrum.Mcp.Protocol;

/// <summary>
/// MCP-specific JSON-RPC error codes.
/// </summary>
public static class McpErrorCode
{
    /// <summary>The HTTP headers do not match the request body.</summary>
    public const int HeaderMismatch = -32020;

    /// <summary>The request needs a client capability that was not declared.</summary>
    public const int MissingRequiredClientCapability = -32021;

    /// <summary>The requested protocol version is not supported.</summary>
    public const int UnsupportedProtocolVersion = -32022;
}
