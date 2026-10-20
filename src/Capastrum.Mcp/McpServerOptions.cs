using Capastrum.Mcp.Protocol;

namespace Capastrum.Mcp;

/// <summary>
/// Configures an <see cref="McpServer"/>.
/// </summary>
public sealed class McpServerOptions
{
    /// <summary>The server name reported by <c>server/discover</c>.</summary>
    public required string Name { get; init; }

    /// <summary>The server version reported by <c>server/discover</c>.</summary>
    public required string Version { get; init; }

    /// <summary>A human-readable server title.</summary>
    public string? Title { get; init; }

    /// <summary>Natural-language guidance for clients.</summary>
    public string? Instructions { get; init; }

    /// <summary>The maximum number of items returned by a list method.</summary>
    public int PageSize { get; init; } = 100;

    /// <summary>Whether the server accepts subscription streams.</summary>
    public bool EnableSubscriptions { get; init; } = true;

    /// <summary>Whether the server sends logging notifications.</summary>
    public bool EnableLogging { get; init; } = true;

    /// <summary>The cache scope reported by <c>server/discover</c>.</summary>
    public McpCacheScope DiscoverCacheScope { get; init; } = McpCacheScope.Private;
}
