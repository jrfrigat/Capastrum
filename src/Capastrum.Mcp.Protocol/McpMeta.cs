using System.Text.Json;
using System.Text.Json.Serialization;
using Capastrum.JsonRpc;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Identifies a client or server implementation.
/// </summary>
public sealed record McpImplementation
{
    /// <summary>The programmatic name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The implementation version.</summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>A human-readable description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The implementation website.</summary>
    [JsonPropertyName("websiteUrl")]
    public string? WebsiteUrl { get; init; }

    /// <summary>Icons the client can display.</summary>
    [JsonPropertyName("icons")]
    public IReadOnlyList<McpIcon>? Icons { get; init; }
}

/// <summary>
/// Describes an icon exposed by a server.
/// </summary>
public sealed record McpIcon
{
    /// <summary>The icon source URI.</summary>
    [JsonPropertyName("src")]
    public required string Src { get; init; }

    /// <summary>The icon MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>The available icon sizes.</summary>
    [JsonPropertyName("sizes")]
    public IReadOnlyList<string>? Sizes { get; init; }

    /// <summary>The intended theme, for example light or dark.</summary>
    [JsonPropertyName("theme")]
    public string? Theme { get; init; }
}

/// <summary>
/// Optional annotations attached to content or resources.
/// </summary>
public sealed record McpAnnotations
{
    /// <summary>The intended audience.</summary>
    [JsonPropertyName("audience")]
    public IReadOnlyList<string>? Audience { get; init; }

    /// <summary>The last modification timestamp.</summary>
    [JsonPropertyName("lastModified")]
    public string? LastModified { get; init; }

    /// <summary>The display priority from 0 to 1.</summary>
    [JsonPropertyName("priority")]
    public double? Priority { get; init; }
}

/// <summary>
/// Capabilities declared by the client on every request.
/// </summary>
public sealed record McpClientCapabilities
{
    /// <summary>Present when the client supports sampling.</summary>
    [JsonPropertyName("sampling")]
    public JsonElement? Sampling { get; init; }

    /// <summary>Present when the client supports roots.</summary>
    [JsonPropertyName("roots")]
    public JsonElement? Roots { get; init; }

    /// <summary>Present when the client supports elicitation.</summary>
    [JsonPropertyName("elicitation")]
    public JsonElement? Elicitation { get; init; }

    /// <summary>Experimental capabilities.</summary>
    [JsonPropertyName("experimental")]
    public IReadOnlyDictionary<string, JsonElement>? Experimental { get; init; }

    /// <summary>Extension capabilities keyed by extension identifier.</summary>
    [JsonPropertyName("extensions")]
    public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }
}

/// <summary>
/// Capabilities declared by the server.
/// </summary>
public sealed record McpServerCapabilities
{
    /// <summary>Present when the server exposes tools.</summary>
    [JsonPropertyName("tools")]
    public JsonElement? Tools { get; init; }

    /// <summary>Present when the server exposes resources.</summary>
    [JsonPropertyName("resources")]
    public JsonElement? Resources { get; init; }

    /// <summary>Present when the server exposes prompts.</summary>
    [JsonPropertyName("prompts")]
    public JsonElement? Prompts { get; init; }

    /// <summary>Present when the server supports argument completion.</summary>
    [JsonPropertyName("completions")]
    public JsonElement? Completions { get; init; }

    /// <summary>Present when the server sends log messages.</summary>
    [JsonPropertyName("logging")]
    public JsonElement? Logging { get; init; }

    /// <summary>Experimental capabilities.</summary>
    [JsonPropertyName("experimental")]
    public IReadOnlyDictionary<string, JsonElement>? Experimental { get; init; }

    /// <summary>Extension capabilities keyed by extension identifier.</summary>
    [JsonPropertyName("extensions")]
    public IReadOnlyDictionary<string, JsonElement>? Extensions { get; init; }
}

/// <summary>
/// Metadata carried by every request.
/// </summary>
public sealed record McpRequestMeta
{
    /// <summary>The protocol version the request targets.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.ProtocolVersion)]
    public required string ProtocolVersion { get; init; }

    /// <summary>The client capabilities.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.ClientCapabilities)]
    public required McpClientCapabilities ClientCapabilities { get; init; }

    /// <summary>The client implementation.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.ClientInfo)]
    public McpImplementation? ClientInfo { get; init; }

    /// <summary>The requested log level.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.LogLevel)]
    public McpLoggingLevel? LogLevel { get; init; }

    /// <summary>The token used to correlate progress notifications.</summary>
    [JsonPropertyName("progressToken")]
    public JsonRpcId? ProgressToken { get; init; }
}

/// <summary>
/// Metadata carried by every result.
/// </summary>
public sealed record McpResultMeta
{
    /// <summary>The server implementation.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.ServerInfo)]
    public McpImplementation? ServerInfo { get; init; }
}

/// <summary>
/// Metadata carried by a subscription stream result.
/// </summary>
public sealed record McpSubscriptionsListenResultMeta
{
    /// <summary>The server implementation.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.ServerInfo)]
    public McpImplementation? ServerInfo { get; init; }

    /// <summary>The JSON-RPC id of the request that opened the stream.</summary>
    [JsonPropertyName(McpProtocol.MetaKeys.SubscriptionId)]
    public required JsonRpcId SubscriptionId { get; init; }
}

/// <summary>
/// Base type for request parameters.
/// </summary>
public abstract record McpRequestParams
{
    /// <summary>Request metadata.</summary>
    [JsonPropertyName("_meta")]
    public required McpRequestMeta Meta { get; init; }
}

/// <summary>
/// Base type for results that complete normally.
/// </summary>
public abstract record McpResult
{
    /// <summary>Result metadata.</summary>
    [JsonPropertyName("_meta")]
    public McpResultMeta? Meta { get; init; }

    /// <summary>The result type.</summary>
    [JsonPropertyName("resultType")]
    public virtual string ResultType => McpProtocol.ResultTypes.Complete;
}

/// <summary>
/// Controls whether a result may be cached across authorization contexts.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<McpCacheScope>))]
public enum McpCacheScope
{
    /// <summary>Cache only within the same authorization context.</summary>
    [JsonStringEnumMemberName("private")]
    Private,

    /// <summary>Cache across authorization contexts.</summary>
    [JsonStringEnumMemberName("public")]
    Public,
}

/// <summary>
/// Log levels used by MCP logging notifications.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<McpLoggingLevel>))]
public enum McpLoggingLevel
{
    /// <summary>Alert level.</summary>
    [JsonStringEnumMemberName("alert")]
    Alert,

    /// <summary>Critical level.</summary>
    [JsonStringEnumMemberName("critical")]
    Critical,

    /// <summary>Debug level.</summary>
    [JsonStringEnumMemberName("debug")]
    Debug,

    /// <summary>Emergency level.</summary>
    [JsonStringEnumMemberName("emergency")]
    Emergency,

    /// <summary>Error level.</summary>
    [JsonStringEnumMemberName("error")]
    Error,

    /// <summary>Informational level.</summary>
    [JsonStringEnumMemberName("info")]
    Info,

    /// <summary>Notice level.</summary>
    [JsonStringEnumMemberName("notice")]
    Notice,

    /// <summary>Warning level.</summary>
    [JsonStringEnumMemberName("warning")]
    Warning,
}

/// <summary>
/// The author role of a prompt or sampling message.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<McpRole>))]
public enum McpRole
{
    /// <summary>The assistant role.</summary>
    [JsonStringEnumMemberName("assistant")]
    Assistant,

    /// <summary>The user role.</summary>
    [JsonStringEnumMemberName("user")]
    User,
}
