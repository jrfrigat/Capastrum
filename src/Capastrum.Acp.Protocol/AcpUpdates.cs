using System.Text.Json;
using System.Text.Json.Serialization;
using Capastrum.JsonRpc;

namespace Capastrum.Acp.Protocol;

/// <summary>
/// A session update notification.
/// </summary>
public sealed record AcpSessionNotification
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The update payload.</summary>
    [JsonPropertyName("update")]
    public required AcpSessionUpdate Update { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An update streamed during a prompt turn.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "sessionUpdate")]
[JsonDerivedType(typeof(AcpUserMessageChunk), "user_message_chunk")]
[JsonDerivedType(typeof(AcpAgentMessageChunk), "agent_message_chunk")]
[JsonDerivedType(typeof(AcpAgentThoughtChunk), "agent_thought_chunk")]
[JsonDerivedType(typeof(AcpToolCallNotification), "tool_call")]
[JsonDerivedType(typeof(AcpToolCallUpdateNotification), "tool_call_update")]
[JsonDerivedType(typeof(AcpPlanUpdate), "plan")]
[JsonDerivedType(typeof(AcpAvailableCommandsUpdate), "available_commands_update")]
[JsonDerivedType(typeof(AcpCurrentModeUpdate), "current_mode_update")]
[JsonDerivedType(typeof(AcpConfigOptionUpdate), "config_option_update")]
[JsonDerivedType(typeof(AcpSessionInfoUpdate), "session_info_update")]
[JsonDerivedType(typeof(AcpUsageUpdate), "usage_update")]
[JsonDerivedType(typeof(AcpNoticeUpdate), "notice")]
[JsonDerivedType(typeof(AcpCompactionUpdate), "compaction_update")]
[JsonDerivedType(typeof(AcpCompactionSummaryChunk), "compaction_summary_chunk")]
public abstract record AcpSessionUpdate
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A chunk of streamed content.
/// </summary>
public abstract record AcpContentChunkUpdate : AcpSessionUpdate
{
    /// <summary>The content block.</summary>
    [JsonPropertyName("content")]
    public required AcpContentBlock Content { get; init; }

    /// <summary>The message identifier.</summary>
    [JsonPropertyName("messageId")]
    public string? MessageId { get; init; }
}

/// <summary>
/// A chunk of the user's message.
/// </summary>
public sealed record AcpUserMessageChunk : AcpContentChunkUpdate;

/// <summary>
/// A chunk of the agent's response.
/// </summary>
public sealed record AcpAgentMessageChunk : AcpContentChunkUpdate;

/// <summary>
/// A chunk of the agent's reasoning.
/// </summary>
public sealed record AcpAgentThoughtChunk : AcpContentChunkUpdate;

/// <summary>
/// A notification that a new tool call started.
/// </summary>
public sealed record AcpToolCallNotification : AcpSessionUpdate
{
    /// <summary>The tool call identifier.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>The tool call title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>The programmatic tool name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The kind of work.</summary>
    [JsonPropertyName("kind")]
    public AcpToolKind? Kind { get; init; }

    /// <summary>The current status.</summary>
    [JsonPropertyName("status")]
    public AcpToolCallStatus? Status { get; init; }

    /// <summary>The produced content.</summary>
    [JsonPropertyName("content")]
    public IReadOnlyList<AcpToolCallContent>? Content { get; init; }

    /// <summary>The affected locations.</summary>
    [JsonPropertyName("locations")]
    public IReadOnlyList<AcpToolCallLocation>? Locations { get; init; }

    /// <summary>The raw tool input.</summary>
    [JsonPropertyName("rawInput")]
    public JsonElement? RawInput { get; init; }

    /// <summary>The raw tool output.</summary>
    [JsonPropertyName("rawOutput")]
    public JsonElement? RawOutput { get; init; }
}

/// <summary>
/// An update to a tool call.
/// </summary>
public sealed record AcpToolCallUpdateNotification : AcpSessionUpdate
{
    /// <summary>The tool call identifier.</summary>
    [JsonPropertyName("toolCallId")]
    public required string ToolCallId { get; init; }

    /// <summary>The kind of work.</summary>
    [JsonPropertyName("kind")]
    public AcpToolKind? Kind { get; init; }

    /// <summary>The current status.</summary>
    [JsonPropertyName("status")]
    public AcpToolCallStatus? Status { get; init; }

    /// <summary>The tool call title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The programmatic tool name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>The produced content.</summary>
    [JsonPropertyName("content")]
    public IReadOnlyList<AcpToolCallContent>? Content { get; init; }

    /// <summary>The affected locations.</summary>
    [JsonPropertyName("locations")]
    public IReadOnlyList<AcpToolCallLocation>? Locations { get; init; }

    /// <summary>The raw tool input.</summary>
    [JsonPropertyName("rawInput")]
    public JsonElement? RawInput { get; init; }

    /// <summary>The raw tool output.</summary>
    [JsonPropertyName("rawOutput")]
    public JsonElement? RawOutput { get; init; }
}

/// <summary>
/// The agent's execution plan.
/// </summary>
public sealed record AcpPlanUpdate : AcpSessionUpdate
{
    /// <summary>The plan entries.</summary>
    [JsonPropertyName("entries")]
    public required IReadOnlyList<AcpPlanEntry> Entries { get; init; }
}

/// <summary>
/// A plan entry.
/// </summary>
public sealed record AcpPlanEntry
{
    /// <summary>The entry text.</summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>The entry priority.</summary>
    [JsonPropertyName("priority")]
    public required AcpPlanEntryPriority Priority { get; init; }

    /// <summary>The entry status.</summary>
    [JsonPropertyName("status")]
    public required AcpPlanEntryStatus Status { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An available command.
/// </summary>
public sealed record AcpAvailableCommand
{
    /// <summary>The command name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The command description.</summary>
    [JsonPropertyName("description")]
    public required string Description { get; init; }

    /// <summary>The command input specification.</summary>
    [JsonPropertyName("input")]
    public JsonElement? Input { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An update to the available commands.
/// </summary>
public sealed record AcpAvailableCommandsUpdate : AcpSessionUpdate
{
    /// <summary>The available commands.</summary>
    [JsonPropertyName("availableCommands")]
    public required IReadOnlyList<AcpAvailableCommand> AvailableCommands { get; init; }
}

/// <summary>
/// An update to the current session mode.
/// </summary>
public sealed record AcpCurrentModeUpdate : AcpSessionUpdate
{
    /// <summary>The current mode identifier.</summary>
    [JsonPropertyName("currentModeId")]
    public required string CurrentModeId { get; init; }
}

/// <summary>
/// An update to the session configuration options.
/// </summary>
public sealed record AcpConfigOptionUpdate : AcpSessionUpdate
{
    /// <summary>The session configuration options.</summary>
    [JsonPropertyName("configOptions")]
    public required IReadOnlyList<AcpSessionConfigOption> ConfigOptions { get; init; }
}

/// <summary>
/// An update to the session metadata.
/// </summary>
public sealed record AcpSessionInfoUpdate : AcpSessionUpdate
{
    /// <summary>The session title.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; init; }

    /// <summary>The last update timestamp.</summary>
    [JsonPropertyName("updatedAt")]
    public string? UpdatedAt { get; init; }
}

/// <summary>
/// A context window and cost update.
/// </summary>
public sealed record AcpUsageUpdate : AcpSessionUpdate
{
    /// <summary>The number of used tokens.</summary>
    [JsonPropertyName("used")]
    public required long Used { get; init; }

    /// <summary>The context window size.</summary>
    [JsonPropertyName("size")]
    public required long Size { get; init; }

    /// <summary>The accumulated cost.</summary>
    [JsonPropertyName("cost")]
    public AcpCost? Cost { get; init; }
}

/// <summary>
/// A monetary cost.
/// </summary>
public sealed record AcpCost
{
    /// <summary>The amount.</summary>
    [JsonPropertyName("amount")]
    public required double Amount { get; init; }

    /// <summary>The currency code.</summary>
    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A session notice.
/// </summary>
public sealed record AcpNoticeUpdate : AcpSessionUpdate
{
    /// <summary>The notice severity.</summary>
    [JsonPropertyName("severity")]
    public required string Severity { get; init; }

    /// <summary>The notice title.</summary>
    [JsonPropertyName("title")]
    public required string Title { get; init; }

    /// <summary>The notice description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; init; }
}

/// <summary>
/// A context compaction update.
/// </summary>
public sealed record AcpCompactionUpdate : AcpSessionUpdate
{
    /// <summary>The compaction identifier.</summary>
    [JsonPropertyName("compactionId")]
    public required string CompactionId { get; init; }

    /// <summary>The compaction status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>The retained summary.</summary>
    [JsonPropertyName("summary")]
    public JsonElement? Summary { get; init; }

    /// <summary>The failure message.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }
}

/// <summary>
/// A content chunk appended to a compaction summary.
/// </summary>
public sealed record AcpCompactionSummaryChunk : AcpSessionUpdate
{
    /// <summary>The compaction identifier.</summary>
    [JsonPropertyName("compactionId")]
    public required string CompactionId { get; init; }

    /// <summary>The content block.</summary>
    [JsonPropertyName("content")]
    public required AcpContentBlock Content { get; init; }
}

/// <summary>
/// A request to cancel the current prompt turn.
/// </summary>
public sealed record AcpCancelNotification
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to cancel a specific JSON-RPC request.
/// </summary>
public sealed record AcpCancelRequestNotification
{
    /// <summary>The request identifier.</summary>
    [JsonPropertyName("requestId")]
    public required JsonRpcId RequestId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An extension request.
/// </summary>
public sealed record AcpExtRequest
{
    /// <summary>The extension method name.</summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>The extension parameters.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// An extension response.
/// </summary>
public sealed record AcpExtResponse
{
    /// <summary>The extension result.</summary>
    [JsonPropertyName("result")]
    public JsonElement? Result { get; init; }
}

/// <summary>
/// An extension notification.
/// </summary>
public sealed record AcpExtNotification
{
    /// <summary>The extension method name.</summary>
    [JsonPropertyName("method")]
    public required string Method { get; init; }

    /// <summary>The extension parameters.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}
