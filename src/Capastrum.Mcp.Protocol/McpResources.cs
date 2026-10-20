using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Describes a resource the server exposes.
/// </summary>
public sealed record McpResource
{
    /// <summary>The resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The programmatic name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The resource MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>The resource size in bytes.</summary>
    [JsonPropertyName("size")]
    public long? Size { get; init; }

    /// <summary>Icons the client can display.</summary>
    [JsonPropertyName("icons")]
    public IReadOnlyList<McpIcon>? Icons { get; init; }

    /// <summary>Optional annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Describes a URI template for a resource.
/// </summary>
public sealed record McpResourceTemplate
{
    /// <summary>The URI template.</summary>
    [JsonPropertyName("uriTemplate")]
    public required string UriTemplate { get; init; }

    /// <summary>The programmatic name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The resource MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>Icons the client can display.</summary>
    [JsonPropertyName("icons")]
    public IReadOnlyList<McpIcon>? Icons { get; init; }

    /// <summary>Optional annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Parameters for <c>resources/list</c>.
/// </summary>
public sealed record McpListResourcesRequestParams : McpRequestParams
{
    /// <summary>The pagination cursor.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }
}

/// <summary>
/// The result of <c>resources/list</c>.
/// </summary>
public sealed record McpListResourcesResult : McpResult
{
    /// <summary>The available resources.</summary>
    [JsonPropertyName("resources")]
    public required IReadOnlyList<McpResource> Resources { get; init; }

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
/// Parameters for <c>resources/templates/list</c>.
/// </summary>
public sealed record McpListResourceTemplatesRequestParams : McpRequestParams
{
    /// <summary>The pagination cursor.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }
}

/// <summary>
/// The result of <c>resources/templates/list</c>.
/// </summary>
public sealed record McpListResourceTemplatesResult : McpResult
{
    /// <summary>The available resource templates.</summary>
    [JsonPropertyName("resourceTemplates")]
    public required IReadOnlyList<McpResourceTemplate> ResourceTemplates { get; init; }

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
/// Parameters for <c>resources/read</c>.
/// </summary>
public sealed record McpReadResourceRequestParams : McpRequestParams
{
    /// <summary>The resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    [JsonPropertyName("inputResponses")]
    public Dictionary<string, JsonElement>? InputResponses { get; init; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    [JsonPropertyName("requestState")]
    public string? RequestState { get; init; }
}

/// <summary>
/// The result of <c>resources/read</c>.
/// </summary>
public sealed record McpReadResourceResult : McpResult
{
    /// <summary>The resource contents.</summary>
    [JsonPropertyName("contents")]
    public required IReadOnlyList<McpResourceContents> Contents { get; init; }

    /// <summary>How long the result may be cached, in milliseconds.</summary>
    [JsonPropertyName("ttlMs")]
    public long? TtlMs { get; init; }

    /// <summary>The cache scope.</summary>
    [JsonPropertyName("cacheScope")]
    public McpCacheScope? CacheScope { get; init; }
}
