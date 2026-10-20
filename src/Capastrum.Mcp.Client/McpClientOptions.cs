using Capastrum.Mcp.Protocol;

namespace Capastrum.Mcp.Client;

/// <summary>
/// Configures an <see cref="McpClient"/>.
/// </summary>
public class McpClientOptions
{
    /// <summary>The client implementation reported to the server.</summary>
    public McpImplementation ClientInfo { get; init; } = new()
    {
        Name = "Capastrum",
        Version = "1.0.0",
    };

    /// <summary>The client capabilities reported to the server.</summary>
    public McpClientCapabilities ClientCapabilities { get; init; } = new();

    /// <summary>Handles sampling input requests from the server.</summary>
    public Func<McpCreateMessageRequestParams, CancellationToken, ValueTask<McpCreateMessageResult>>? SamplingHandler { get; init; }

    /// <summary>Handles roots input requests from the server.</summary>
    public Func<CancellationToken, ValueTask<McpListRootsResult>>? RootsHandler { get; init; }

    /// <summary>Handles elicitation input requests from the server.</summary>
    public Func<McpElicitRequestParams, CancellationToken, ValueTask<McpElicitResult>>? ElicitationHandler { get; init; }
}

/// <summary>
/// Configures an MCP client that launches a child process over stdio.
/// </summary>
public sealed class StdioMcpClientOptions : McpClientOptions
{
    /// <summary>The executable to launch.</summary>
    public required string Command { get; init; }

    /// <summary>The command arguments.</summary>
    public IReadOnlyList<string> Arguments { get; init; } = [];

    /// <summary>Additional environment variables.</summary>
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>The working directory.</summary>
    public string? WorkingDirectory { get; init; }
}

/// <summary>
/// Configures an MCP client that posts JSON-RPC messages to an HTTP endpoint.
/// </summary>
public sealed class HttpMcpClientOptions : McpClientOptions
{
    /// <summary>The endpoint URL.</summary>
    public required Uri Endpoint { get; init; }

    /// <summary>An optional caller-owned HTTP client.</summary>
    public HttpClient? HttpClient { get; init; }

    /// <summary>Additional request headers.</summary>
    public IReadOnlyDictionary<string, string>? Headers { get; init; }
}
