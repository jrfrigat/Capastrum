using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Describes a prompt the server exposes.
/// </summary>
public sealed record McpPrompt
{
    /// <summary>The programmatic prompt name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The prompt arguments.</summary>
    [JsonPropertyName("arguments")]
    public IReadOnlyList<McpPromptArgument>? Arguments { get; init; }

    /// <summary>Icons the client can display.</summary>
    [JsonPropertyName("icons")]
    public IReadOnlyList<McpIcon>? Icons { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Describes a prompt argument.
/// </summary>
public sealed record McpPromptArgument
{
    /// <summary>The argument name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Whether the argument is required.</summary>
    [JsonPropertyName("required")]
    public bool? Required { get; init; }
}

/// <summary>
/// Parameters for <c>prompts/list</c>.
/// </summary>
public sealed record McpListPromptsRequestParams : McpRequestParams
{
    /// <summary>The pagination cursor.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }
}

/// <summary>
/// The result of <c>prompts/list</c>.
/// </summary>
public sealed record McpListPromptsResult : McpResult
{
    /// <summary>The available prompts.</summary>
    [JsonPropertyName("prompts")]
    public required IReadOnlyList<McpPrompt> Prompts { get; init; }

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
/// Parameters for <c>prompts/get</c>.
/// </summary>
public sealed record McpGetPromptRequestParams : McpRequestParams
{
    /// <summary>The prompt name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The prompt arguments.</summary>
    [JsonPropertyName("arguments")]
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    [JsonPropertyName("inputResponses")]
    public Dictionary<string, JsonElement>? InputResponses { get; init; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    [JsonPropertyName("requestState")]
    public string? RequestState { get; init; }
}

/// <summary>
/// The result of <c>prompts/get</c>.
/// </summary>
public sealed record McpGetPromptResult : McpResult
{
    /// <summary>An optional prompt description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The prompt messages.</summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<McpPromptMessage> Messages { get; init; }
}
