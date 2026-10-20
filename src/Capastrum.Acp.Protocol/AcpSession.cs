using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Acp.Protocol;

/// <summary>
/// The client's initialization request.
/// </summary>
public sealed record AcpInitializeRequest
{
    /// <summary>The latest protocol version supported by the client.</summary>
    [JsonPropertyName("protocolVersion")]
    public required int ProtocolVersion { get; init; }

    /// <summary>The client capabilities.</summary>
    [JsonPropertyName("clientCapabilities")]
    public AcpClientCapabilities? ClientCapabilities { get; init; }

    /// <summary>The client implementation.</summary>
    [JsonPropertyName("clientInfo")]
    public AcpImplementation? ClientInfo { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The agent's initialization response.
/// </summary>
public sealed record AcpInitializeResponse
{
    /// <summary>The negotiated protocol version.</summary>
    [JsonPropertyName("protocolVersion")]
    public required int ProtocolVersion { get; init; }

    /// <summary>The agent capabilities.</summary>
    [JsonPropertyName("agentCapabilities")]
    public AcpAgentCapabilities? AgentCapabilities { get; init; }

    /// <summary>The available authentication methods.</summary>
    [JsonPropertyName("authMethods")]
    public IReadOnlyList<AcpAuthMethod>? AuthMethods { get; init; }

    /// <summary>The agent implementation.</summary>
    [JsonPropertyName("agentInfo")]
    public AcpImplementation? AgentInfo { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An authentication request.
/// </summary>
public sealed record AcpAuthenticateRequest
{
    /// <summary>The authentication method identifier.</summary>
    [JsonPropertyName("methodId")]
    public required string MethodId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of authentication.
/// </summary>
public sealed record AcpAuthenticateResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A logout request.
/// </summary>
public sealed record AcpLogoutRequest
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of a logout.
/// </summary>
public sealed record AcpLogoutResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to create a session.
/// </summary>
public sealed record AcpNewSessionRequest
{
    /// <summary>The working directory.</summary>
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    /// <summary>Additional directories available to the agent.</summary>
    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    /// <summary>The MCP servers to connect.</summary>
    [JsonPropertyName("mcpServers")]
    public required IReadOnlyList<AcpMcpServer> McpServers { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The created session.
/// </summary>
public sealed record AcpNewSessionResponse
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The session modes.</summary>
    [JsonPropertyName("modes")]
    public AcpSessionModeState? Modes { get; init; }

    /// <summary>The session configuration options.</summary>
    [JsonPropertyName("configOptions")]
    public IReadOnlyList<AcpSessionConfigOption>? ConfigOptions { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The available session modes.
/// </summary>
public sealed record AcpSessionModeState
{
    /// <summary>The current mode identifier.</summary>
    [JsonPropertyName("currentModeId")]
    public required string CurrentModeId { get; init; }

    /// <summary>The available modes.</summary>
    [JsonPropertyName("availableModes")]
    public required IReadOnlyList<AcpSessionMode> AvailableModes { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A session mode.
/// </summary>
public sealed record AcpSessionMode
{
    /// <summary>The mode identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The mode name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The mode description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A session configuration option.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AcpSessionConfigSelect), "select")]
[JsonDerivedType(typeof(AcpSessionConfigBoolean), "boolean")]
public abstract record AcpSessionConfigOption
{
    /// <summary>The option identifier.</summary>
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    /// <summary>The option name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The option description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }

    /// <summary>The option category.</summary>
    [JsonPropertyName("category")]
    public string? Category { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A single-value session configuration option.
/// </summary>
public sealed record AcpSessionConfigSelect : AcpSessionConfigOption
{
    /// <summary>The current value identifier.</summary>
    [JsonPropertyName("currentValue")]
    public required string CurrentValue { get; init; }

    /// <summary>The available values, flat or grouped.</summary>
    [JsonPropertyName("options")]
    public required JsonElement Options { get; init; }
}

/// <summary>
/// A boolean session configuration option.
/// </summary>
public sealed record AcpSessionConfigBoolean : AcpSessionConfigOption
{
    /// <summary>The current value.</summary>
    [JsonPropertyName("currentValue")]
    public required bool CurrentValue { get; init; }
}

/// <summary>
/// A request to load a session.
/// </summary>
public sealed record AcpLoadSessionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The working directory.</summary>
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    /// <summary>The MCP servers to connect.</summary>
    [JsonPropertyName("mcpServers")]
    public required IReadOnlyList<AcpMcpServer> McpServers { get; init; }

    /// <summary>Additional directories available to the agent.</summary>
    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The loaded session.
/// </summary>
public sealed record AcpLoadSessionResponse
{
    /// <summary>The session modes.</summary>
    [JsonPropertyName("modes")]
    public AcpSessionModeState? Modes { get; init; }

    /// <summary>The session configuration options.</summary>
    [JsonPropertyName("configOptions")]
    public IReadOnlyList<AcpSessionConfigOption>? ConfigOptions { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to resume a session.
/// </summary>
public sealed record AcpResumeSessionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The working directory.</summary>
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    /// <summary>Additional directories available to the agent.</summary>
    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    /// <summary>The MCP servers to connect.</summary>
    [JsonPropertyName("mcpServers")]
    public IReadOnlyList<AcpMcpServer>? McpServers { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The resumed session.
/// </summary>
public sealed record AcpResumeSessionResponse
{
    /// <summary>The session modes.</summary>
    [JsonPropertyName("modes")]
    public AcpSessionModeState? Modes { get; init; }

    /// <summary>The session configuration options.</summary>
    [JsonPropertyName("configOptions")]
    public IReadOnlyList<AcpSessionConfigOption>? ConfigOptions { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to list sessions.
/// </summary>
public sealed record AcpListSessionsRequest
{
    /// <summary>The working directory filter.</summary>
    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    /// <summary>The pagination cursor.</summary>
    [JsonPropertyName("cursor")]
    public string? Cursor { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The session list.
/// </summary>
public sealed record AcpListSessionsResponse
{
    /// <summary>The sessions.</summary>
    [JsonPropertyName("sessions")]
    public required IReadOnlyList<AcpSessionInfo> Sessions { get; init; }

    /// <summary>The cursor of the next page.</summary>
    [JsonPropertyName("nextCursor")]
    public string? NextCursor { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Session metadata.
/// </summary>
public sealed record AcpSessionInfo
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The working directory.</summary>
    [JsonPropertyName("cwd")]
    public required string Cwd { get; init; }

    /// <summary>Additional directories available to the agent.</summary>
    [JsonPropertyName("additionalDirectories")]
    public IReadOnlyList<string>? AdditionalDirectories { get; init; }

    /// <summary>The session title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The last update timestamp.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to close a session.
/// </summary>
public sealed record AcpCloseSessionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of closing a session.
/// </summary>
public sealed record AcpCloseSessionResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to delete a session.
/// </summary>
public sealed record AcpDeleteSessionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of deleting a session.
/// </summary>
public sealed record AcpDeleteSessionResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to set the session mode.
/// </summary>
public sealed record AcpSetSessionModeRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The mode identifier.</summary>
    [JsonPropertyName("modeId")]
    public required string ModeId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of setting the session mode.
/// </summary>
public sealed record AcpSetSessionModeResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to set a session configuration option.
/// </summary>
public sealed record AcpSetSessionConfigOptionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The configuration identifier.</summary>
    [JsonPropertyName("configId")]
    public required string ConfigId { get; init; }

    /// <summary>The new value.</summary>
    [JsonPropertyName("value")]
    public JsonElement? Value { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The updated session configuration options.
/// </summary>
public sealed record AcpSetSessionConfigOptionResponse
{
    /// <summary>The session configuration options.</summary>
    [JsonPropertyName("configOptions")]
    public IReadOnlyList<AcpSessionConfigOption>? ConfigOptions { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to process a prompt turn.
/// </summary>
public sealed record AcpPromptRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The prompt content.</summary>
    [JsonPropertyName("prompt")]
    public required IReadOnlyList<AcpContentBlock> Prompt { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of a prompt turn.
/// </summary>
public sealed record AcpPromptResponse
{
    /// <summary>The reason the turn stopped.</summary>
    [JsonPropertyName("stopReason")]
    public required AcpStopReason StopReason { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}
