using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Describes a tool the server exposes.
/// </summary>
public sealed record McpTool
{
    /// <summary>The programmatic tool name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A human-readable description for the model.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The JSON Schema of the tool arguments.</summary>
    [JsonPropertyName("inputSchema")]
    public required JsonElement InputSchema { get; init; }

    /// <summary>The JSON Schema of the structured result.</summary>
    [JsonPropertyName("outputSchema")]
    public JsonElement? OutputSchema { get; init; }

    /// <summary>Optional annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpToolAnnotations? Annotations { get; init; }

    /// <summary>Icons the client can display.</summary>
    [JsonPropertyName("icons")]
    public IReadOnlyList<McpIcon>? Icons { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Hints about a tool's behavior.
/// </summary>
public sealed record McpToolAnnotations
{
    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The tool does not modify its environment.</summary>
    [JsonPropertyName("readOnlyHint")]
    public bool? ReadOnlyHint { get; init; }

    /// <summary>The tool may perform destructive updates.</summary>
    [JsonPropertyName("destructiveHint")]
    public bool? DestructiveHint { get; init; }

    /// <summary>Repeating the call has no additional effect.</summary>
    [JsonPropertyName("idempotentHint")]
    public bool? IdempotentHint { get; init; }

    /// <summary>The tool may interact with external entities.</summary>
    [JsonPropertyName("openWorldHint")]
    public bool? OpenWorldHint { get; init; }
}

/// <summary>
/// Parameters for <c>tools/list</c>.
/// </summary>
public sealed record McpListToolsRequestParams : McpRequestParams
{
    /// <summary>The pagination cursor.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }
}

/// <summary>
/// The result of <c>tools/list</c>.
/// </summary>
public sealed record McpListToolsResult : McpResult
{
    /// <summary>The available tools.</summary>
    [JsonPropertyName("tools")]
    public required IReadOnlyList<McpTool> Tools { get; init; }

    /// <summary>The cursor of the next page.</summary>
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; init; }

    /// <summary>How long the result may be cached, in milliseconds.</summary>
    [JsonPropertyName("ttlMs")]
    public long? TtlMs { get; init; }

    /// <summary>The cache scope.</summary>
    [JsonPropertyName("cacheScope")]
    public McpCacheScope? CacheScope { get; init; }
}

/// <summary>
/// Parameters for <c>tools/call</c>.
/// </summary>
public sealed record McpCallToolRequestParams : McpRequestParams
{
    /// <summary>The tool name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The tool arguments.</summary>
    [JsonPropertyName("arguments")]
    public JsonElement? Arguments { get; init; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    [JsonPropertyName("inputResponses")]
    public Dictionary<string, JsonElement>? InputResponses { get; init; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    [JsonPropertyName("requestState")]
    public string? RequestState { get; init; }
}

/// <summary>
/// The result of <c>tools/call</c>.
/// </summary>
public sealed record McpCallToolResult : McpResult
{
    /// <summary>The unstructured tool result.</summary>
    [JsonPropertyName("content")]
    public required IReadOnlyList<McpContentBlock> Content { get; init; }

    /// <summary>Optional structured tool result.</summary>
    [JsonPropertyName("structuredContent")]
    public JsonElement? StructuredContent { get; init; }

    /// <summary>Whether the tool call ended in an error.</summary>
    [JsonPropertyName("isError")]
    public bool? IsError { get; init; }
}
