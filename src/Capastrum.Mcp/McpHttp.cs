namespace Capastrum.Mcp;

/// <summary>
/// A transport-neutral HTTP request passed to <see cref="McpServer.HandleHttpRequestAsync"/>.
/// </summary>
public sealed record McpHttpRequest
{
    /// <summary>The HTTP method.</summary>
    public required string Method { get; init; }

    /// <summary>The request headers.</summary>
    public required IReadOnlyDictionary<string, string> Headers { get; init; }

    /// <summary>The request body.</summary>
    public required ReadOnlyMemory<byte> Body { get; init; }
}

/// <summary>
/// A transport-neutral HTTP response returned by <see cref="McpServer.HandleHttpRequestAsync"/>.
/// </summary>
public sealed record McpHttpResponse
{
    /// <summary>The HTTP status code.</summary>
    public required int StatusCode { get; init; }

    /// <summary>The response content type.</summary>
    public string? ContentType { get; init; }

    /// <summary>The response body.</summary>
    public ReadOnlyMemory<byte> Body { get; init; }
}
