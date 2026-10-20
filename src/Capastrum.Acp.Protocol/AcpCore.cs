using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Acp.Protocol;

/// <summary>
/// Identifies an agent or client implementation.
/// </summary>
public sealed record AcpImplementation
{
    /// <summary>The implementation name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The implementation version.</summary>
    [JsonPropertyName("version")]
    public required string Version { get; init; }

    /// <summary>A human-readable title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Display annotations attached to content.
/// </summary>
public sealed record AcpAnnotations
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

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Capabilities declared by the client.
/// </summary>
public sealed record AcpClientCapabilities
{
    /// <summary>File system capabilities.</summary>
    [JsonPropertyName("fs")]
    public AcpFileSystemCapabilities? FileSystem { get; init; }

    /// <summary>Whether the client supports terminals.</summary>
    [JsonPropertyName("terminal")]
    public bool? Terminal { get; init; }

    /// <summary>Session capabilities.</summary>
    [JsonPropertyName("session")]
    public AcpClientSessionCapabilities? Session { get; init; }

    /// <summary>Authentication capabilities.</summary>
    [JsonPropertyName("auth")]
    public AcpClientAuthCapabilities? Auth { get; init; }

    /// <summary>Elicitation capabilities.</summary>
    [JsonPropertyName("elicitation")]
    public JsonElement? Elicitation { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// File system capabilities.
/// </summary>
public sealed record AcpFileSystemCapabilities
{
    /// <summary>Whether the client can read text files.</summary>
    [JsonPropertyName("readTextFile")]
    public bool? ReadTextFile { get; init; }

    /// <summary>Whether the client can write text files.</summary>
    [JsonPropertyName("writeTextFile")]
    public bool? WriteTextFile { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Session capabilities declared by the client.
/// </summary>
public sealed record AcpClientSessionCapabilities
{
    /// <summary>Context compaction support.</summary>
    [JsonPropertyName("compaction")]
    public JsonElement? Compaction { get; init; }

    /// <summary>Session configuration option support.</summary>
    [JsonPropertyName("configOptions")]
    public JsonElement? ConfigOptions { get; init; }

    /// <summary>Session notice support.</summary>
    [JsonPropertyName("notices")]
    public JsonElement? Notices { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Authentication capabilities declared by the client.
/// </summary>
public sealed record AcpClientAuthCapabilities
{
    /// <summary>Whether the client supports terminal authentication.</summary>
    [JsonPropertyName("terminal")]
    public bool? Terminal { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Capabilities declared by the agent.
/// </summary>
public sealed record AcpAgentCapabilities
{
    /// <summary>Whether the agent can load sessions.</summary>
    [JsonPropertyName("loadSession")]
    public bool? LoadSession { get; init; }

    /// <summary>Prompt capabilities.</summary>
    [JsonPropertyName("promptCapabilities")]
    public AcpPromptCapabilities? PromptCapabilities { get; init; }

    /// <summary>MCP transport capabilities.</summary>
    [JsonPropertyName("mcpCapabilities")]
    public AcpMcpCapabilities? McpCapabilities { get; init; }

    /// <summary>Session capabilities.</summary>
    [JsonPropertyName("sessionCapabilities")]
    public AcpAgentSessionCapabilities? SessionCapabilities { get; init; }

    /// <summary>Authentication support.</summary>
    [JsonPropertyName("auth")]
    public JsonElement? Auth { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Prompt content capabilities.
/// </summary>
public sealed record AcpPromptCapabilities
{
    /// <summary>Whether the agent accepts image content.</summary>
    [JsonPropertyName("image")]
    public bool? Image { get; init; }

    /// <summary>Whether the agent accepts audio content.</summary>
    [JsonPropertyName("audio")]
    public bool? Audio { get; init; }

    /// <summary>Whether the agent accepts embedded context.</summary>
    [JsonPropertyName("embeddedContext")]
    public bool? EmbeddedContext { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// MCP transport capabilities.
/// </summary>
public sealed record AcpMcpCapabilities
{
    /// <summary>Whether the agent supports HTTP MCP servers.</summary>
    [JsonPropertyName("http")]
    public bool? Http { get; init; }

    /// <summary>Whether the agent supports SSE MCP servers.</summary>
    [JsonPropertyName("sse")]
    public bool? Sse { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Session capabilities declared by the agent.
/// </summary>
public sealed record AcpAgentSessionCapabilities
{
    /// <summary>Session listing support.</summary>
    [JsonPropertyName("list")]
    public JsonElement? List { get; init; }

    /// <summary>Session deletion support.</summary>
    [JsonPropertyName("delete")]
    public JsonElement? Delete { get; init; }

    /// <summary>Additional directory support.</summary>
    [JsonPropertyName("additionalDirectories")]
    public JsonElement? AdditionalDirectories { get; init; }

    /// <summary>Session resume support.</summary>
    [JsonPropertyName("resume")]
    public JsonElement? Resume { get; init; }

    /// <summary>Session close support.</summary>
    [JsonPropertyName("close")]
    public JsonElement? Close { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An authentication method offered by the agent.
/// </summary>
public sealed record AcpAuthMethod
{
    /// <summary>The method identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The method name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The method description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The method type; null means agent.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>Terminal arguments.</summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    /// <summary>Terminal environment variables.</summary>
    [JsonPropertyName("env")]
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An MCP server configuration.
/// </summary>
public sealed record AcpMcpServer
{
    /// <summary>The server name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The transport type; null means stdio.</summary>
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    /// <summary>The stdio command.</summary>
    [JsonPropertyName("command")]
    public string? Command { get; init; }

    /// <summary>The stdio arguments.</summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    /// <summary>The stdio environment variables.</summary>
    [JsonPropertyName("env")]
    public IReadOnlyDictionary<string, string>? Environment { get; init; }

    /// <summary>The HTTP or SSE URL.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>The HTTP or SSE headers.</summary>
    [JsonPropertyName("headers")]
    public IReadOnlyList<AcpHttpHeader>? Headers { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An HTTP header sent to an MCP server.
/// </summary>
public sealed record AcpHttpHeader
{
    /// <summary>The header name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The header value.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}

/// <summary>
/// An environment variable passed to a terminal or MCP server.
/// </summary>
public sealed record AcpEnvVariable
{
    /// <summary>The variable name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The variable value.</summary>
    [JsonPropertyName("value")]
    public required string Value { get; init; }
}
