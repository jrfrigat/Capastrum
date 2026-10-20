using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Parameters for <c>server/discover</c>.
/// </summary>
public sealed record McpDiscoverRequestParams : McpRequestParams;

/// <summary>
/// The result of <c>server/discover</c>.
/// </summary>
public sealed record McpDiscoverResult : McpResult
{
    /// <summary>The protocol versions the server supports.</summary>
    [JsonPropertyName("supportedVersions")]
    public required IReadOnlyList<string> SupportedVersions { get; init; }

    /// <summary>The server capabilities.</summary>
    [JsonPropertyName("capabilities")]
    public required McpServerCapabilities Capabilities { get; init; }

    /// <summary>Natural-language guidance about the server.</summary>
    [JsonPropertyName("instructions")]
    public string? Instructions { get; init; }

    /// <summary>How long the result may be cached, in milliseconds.</summary>
    [JsonPropertyName("ttlMs")]
    public long? TtlMs { get; init; }

    /// <summary>The cache scope.</summary>
    [JsonPropertyName("cacheScope")]
    public McpCacheScope? CacheScope { get; init; }
}

/// <summary>
/// Parameters for <c>completion/complete</c>.
/// </summary>
public sealed record McpCompleteRequestParams : McpRequestParams
{
    /// <summary>The prompt or resource template reference.</summary>
    [JsonPropertyName("ref")]
    public required McpCompletionReference Ref { get; init; }

    /// <summary>The argument being completed.</summary>
    [JsonPropertyName("argument")]
    public required McpCompletionArgument Argument { get; init; }

    /// <summary>Previously resolved variables.</summary>
    [JsonPropertyName("context")]
    public McpCompletionContext? Context { get; init; }
}

/// <summary>
/// A reference to a prompt or a resource template.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpPromptReference), "ref/prompt")]
[JsonDerivedType(typeof(McpResourceTemplateReference), "ref/resource")]
public abstract record McpCompletionReference;

/// <summary>
/// A reference to a prompt.
/// </summary>
public sealed record McpPromptReference : McpCompletionReference
{
    /// <summary>The prompt name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }
}

/// <summary>
/// A reference to a resource template.
/// </summary>
public sealed record McpResourceTemplateReference : McpCompletionReference
{
    /// <summary>The URI or URI template.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }
}

/// <summary>
/// The argument being completed.
/// </summary>
public sealed record McpCompletionArgument
{
    /// <summary>The argument name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The value to match.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

/// <summary>
/// Previously resolved template or prompt variables.
/// </summary>
public sealed record McpCompletionContext
{
    /// <summary>The resolved variables.</summary>
    [JsonPropertyName("arguments")]
    public IReadOnlyDictionary<string, string>? Arguments { get; init; }
}

/// <summary>
/// Completion values.
/// </summary>
public sealed record McpCompletion
{
    /// <summary>The completion values.</summary>
    [JsonPropertyName("values")]
    public required IReadOnlyList<string> Values { get; init; }

    /// <summary>The total number of completion options.</summary>
    [JsonPropertyName("total")]
    public int? Total { get; init; }

    /// <summary>Whether more options exist beyond this response.</summary>
    [JsonPropertyName("hasMore")]
    public bool? HasMore { get; init; }
}

/// <summary>
/// The result of <c>completion/complete</c>.
/// </summary>
public sealed record McpCompleteResult : McpResult
{
    /// <summary>The completion values.</summary>
    [JsonPropertyName("completion")]
    public required McpCompletion Completion { get; init; }
}
