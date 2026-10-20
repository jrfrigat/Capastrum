using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// A content block returned by a tool, prompt or sampling response.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(McpTextContent), "text")]
[JsonDerivedType(typeof(McpImageContent), "image")]
[JsonDerivedType(typeof(McpAudioContent), "audio")]
[JsonDerivedType(typeof(McpResourceLink), "resource_link")]
[JsonDerivedType(typeof(McpEmbeddedResource), "resource")]
[JsonDerivedType(typeof(McpToolUseContent), "tool_use")]
[JsonDerivedType(typeof(McpToolResultContent), "tool_result")]
public abstract record McpContentBlock
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Plain text content.
/// </summary>
public sealed record McpTextContent : McpContentBlock
{
    /// <summary>The text.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Base64-encoded image content.
/// </summary>
public sealed record McpImageContent : McpContentBlock
{
    /// <summary>The base64-encoded image data.</summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>The image MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Base64-encoded audio content.
/// </summary>
public sealed record McpAudioContent : McpContentBlock
{
    /// <summary>The base64-encoded audio data.</summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>The audio MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// A link to a resource.
/// </summary>
public sealed record McpResourceLink : McpContentBlock
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

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Resource contents embedded directly in a content block.
/// </summary>
public sealed record McpEmbeddedResource : McpContentBlock
{
    /// <summary>The embedded resource contents.</summary>
    [JsonPropertyName("resource")]
    public required McpResourceContents Resource { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public McpAnnotations? Annotations { get; init; }
}

/// <summary>
/// A request from the model to use a tool.
/// </summary>
public sealed record McpToolUseContent : McpContentBlock
{
    /// <summary>The tool use identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The tool name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The tool input.</summary>
    [JsonPropertyName("input")]
    public required JsonElement Input { get; init; }
}

/// <summary>
/// The result of a tool use, returned to the model.
/// </summary>
public sealed record McpToolResultContent : McpContentBlock
{
    /// <summary>The tool use identifier this result belongs to.</summary>
    [JsonPropertyName("toolUseId")]
    public required string ToolUseId { get; init; }

    /// <summary>The tool result content.</summary>
    [JsonPropertyName("content")]
    public required IReadOnlyList<McpContentBlock> Content { get; init; }

    /// <summary>Whether the tool call ended in an error.</summary>
    [JsonPropertyName("isError")]
    public bool? IsError { get; init; }

    /// <summary>Optional structured result.</summary>
    [JsonPropertyName("structuredContent")]
    public JsonElement? StructuredContent { get; init; }
}

/// <summary>
/// The contents of a resource: either text or a base64 blob.
/// </summary>
public sealed record McpResourceContents
{
    /// <summary>The resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The resource MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>The text contents, when the resource is textual.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>The base64-encoded contents, when the resource is binary.</summary>
    [JsonPropertyName("blob")]
    public string? Blob { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A message returned by a prompt.
/// </summary>
public sealed record McpPromptMessage
{
    /// <summary>The message author role.</summary>
    [JsonPropertyName("role")]
    public required McpRole Role { get; init; }

    /// <summary>The message content.</summary>
    [JsonPropertyName("content")]
    public required McpContentBlock Content { get; init; }
}
