using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Acp.Protocol;

/// <summary>
/// A content block in a prompt, message or tool result.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AcpTextContent), "text")]
[JsonDerivedType(typeof(AcpImageContent), "image")]
[JsonDerivedType(typeof(AcpAudioContent), "audio")]
[JsonDerivedType(typeof(AcpResourceLink), "resource_link")]
[JsonDerivedType(typeof(AcpEmbeddedResource), "resource")]
public abstract record AcpContentBlock
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Text content.
/// </summary>
public sealed record AcpTextContent : AcpContentBlock
{
    /// <summary>The text.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Image content.
/// </summary>
public sealed record AcpImageContent : AcpContentBlock
{
    /// <summary>The base64-encoded image data.</summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>The image MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>An optional image URI.</summary>
    [JsonPropertyName("uri")]
    public string? Uri { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Audio content.
/// </summary>
public sealed record AcpAudioContent : AcpContentBlock
{
    /// <summary>The base64-encoded audio data.</summary>
    [JsonPropertyName("data")]
    public required string Data { get; init; }

    /// <summary>The audio MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; init; }
}

/// <summary>
/// A link to a resource.
/// </summary>
public sealed record AcpResourceLink : AcpContentBlock
{
    /// <summary>The resource name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

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

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; init; }
}

/// <summary>
/// Resource contents embedded in a message.
/// </summary>
public sealed record AcpEmbeddedResource : AcpContentBlock
{
    /// <summary>The embedded resource.</summary>
    [JsonPropertyName("resource")]
    public required AcpResourceContents Resource { get; init; }

    /// <summary>Optional display annotations.</summary>
    [JsonPropertyName("annotations")]
    public AcpAnnotations? Annotations { get; init; }
}

/// <summary>
/// The contents of an embedded resource.
/// </summary>
public sealed record AcpResourceContents
{
    /// <summary>The resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>The resource MIME type.</summary>
    [JsonPropertyName("mimeType")]
    public string? MimeType { get; init; }

    /// <summary>The text contents.</summary>
    [JsonPropertyName("text")]
    public string? Text { get; init; }

    /// <summary>The base64-encoded binary contents.</summary>
    [JsonPropertyName("blob")]
    public string? Blob { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Content produced by a tool call.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(AcpContentToolCallContent), "content")]
[JsonDerivedType(typeof(AcpDiff), "diff")]
[JsonDerivedType(typeof(AcpTerminalRef), "terminal")]
public abstract record AcpToolCallContent
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A standard content block produced by a tool call.
/// </summary>
public sealed record AcpContentToolCallContent : AcpToolCallContent
{
    /// <summary>The content block.</summary>
    [JsonPropertyName("content")]
    public required AcpContentBlock Content { get; init; }
}

/// <summary>
/// A file modification shown as a diff.
/// </summary>
public sealed record AcpDiff : AcpToolCallContent
{
    /// <summary>The modified file path.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>The previous text, when known.</summary>
    [JsonPropertyName("oldText")]
    public string? OldText { get; init; }

    /// <summary>The new text.</summary>
    [JsonPropertyName("newText")]
    public required string NewText { get; init; }
}

/// <summary>
/// A reference to a terminal created by the client.
/// </summary>
public sealed record AcpTerminalRef : AcpToolCallContent
{
    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }
}

/// <summary>
/// A tool call initiated by the agent.
/// </summary>
public sealed record AcpToolCall
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

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// An update to a tool call.
/// </summary>
public sealed record AcpToolCallUpdate
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

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A location affected by a tool call.
/// </summary>
public sealed record AcpToolCallLocation
{
    /// <summary>The file path.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>The line number.</summary>
    [JsonPropertyName("line")]
    public int? Line { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A permission option offered to the user.
/// </summary>
public sealed record AcpPermissionOption
{
    /// <summary>The option identifier.</summary>
    [JsonPropertyName("optionId")]
    public required string OptionId { get; init; }

    /// <summary>The option name.</summary>
    [JsonPropertyName("name")]
    public required string Name { get; init; }

    /// <summary>The option kind.</summary>
    [JsonPropertyName("kind")]
    public required AcpPermissionOptionKind Kind { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request from the agent for permission to run a tool call.
/// </summary>
public sealed record AcpRequestPermissionRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The tool call being authorized.</summary>
    [JsonPropertyName("toolCall")]
    public required AcpToolCallUpdate ToolCall { get; init; }

    /// <summary>The options the client can present.</summary>
    [JsonPropertyName("options")]
    public required IReadOnlyList<AcpPermissionOption> Options { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The client's decision for a permission request.
/// </summary>
public sealed record AcpRequestPermissionResponse
{
    /// <summary>The permission outcome.</summary>
    [JsonPropertyName("outcome")]
    public required AcpRequestPermissionOutcome Outcome { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The outcome of a permission request.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "outcome")]
[JsonDerivedType(typeof(AcpCancelledPermissionOutcome), "cancelled")]
[JsonDerivedType(typeof(AcpSelectedPermissionOutcome), "selected")]
public abstract record AcpRequestPermissionOutcome;

/// <summary>
/// The prompt turn was cancelled before the user responded.
/// </summary>
public sealed record AcpCancelledPermissionOutcome : AcpRequestPermissionOutcome;

/// <summary>
/// The user selected a permission option.
/// </summary>
public sealed record AcpSelectedPermissionOutcome : AcpRequestPermissionOutcome
{
    /// <summary>The selected option identifier.</summary>
    [JsonPropertyName("optionId")]
    public required string OptionId { get; init; }
}

/// <summary>
/// A request to read a text file.
/// </summary>
public sealed record AcpReadTextFileRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The file path.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>The first line to read.</summary>
    [JsonPropertyName("line")]
    public int? Line { get; init; }

    /// <summary>The maximum number of lines to read.</summary>
    [JsonPropertyName("limit")]
    public int? Limit { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The text file contents.
/// </summary>
public sealed record AcpReadTextFileResponse
{
    /// <summary>The file contents.</summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to write a text file.
/// </summary>
public sealed record AcpWriteTextFileRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The file path.</summary>
    [JsonPropertyName("path")]
    public required string Path { get; init; }

    /// <summary>The file contents.</summary>
    [JsonPropertyName("content")]
    public required string Content { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of a text file write.
/// </summary>
public sealed record AcpWriteTextFileResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to create a terminal.
/// </summary>
public sealed record AcpCreateTerminalRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The command to run.</summary>
    [JsonPropertyName("command")]
    public required string Command { get; init; }

    /// <summary>The command arguments.</summary>
    [JsonPropertyName("args")]
    public IReadOnlyList<string>? Args { get; init; }

    /// <summary>The environment variables.</summary>
    [JsonPropertyName("env")]
    public IReadOnlyList<AcpEnvVariable>? Environment { get; init; }

    /// <summary>The working directory.</summary>
    [JsonPropertyName("cwd")]
    public string? Cwd { get; init; }

    /// <summary>The maximum number of output bytes kept.</summary>
    [JsonPropertyName("outputByteLimit")]
    public int? OutputByteLimit { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The created terminal identifier.
/// </summary>
public sealed record AcpCreateTerminalResponse
{
    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to read terminal output.
/// </summary>
public sealed record AcpTerminalOutputRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The terminal output.
/// </summary>
public sealed record AcpTerminalOutputResponse
{
    /// <summary>The output text.</summary>
    [JsonPropertyName("output")]
    public required string Output { get; init; }

    /// <summary>Whether the output was truncated.</summary>
    [JsonPropertyName("truncated")]
    public required bool Truncated { get; init; }

    /// <summary>The exit status, when the terminal has exited.</summary>
    [JsonPropertyName("exitStatus")]
    public AcpTerminalExitStatus? ExitStatus { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The exit status of a terminal.
/// </summary>
public sealed record AcpTerminalExitStatus
{
    /// <summary>The exit code.</summary>
    [JsonPropertyName("exitCode")]
    public int? ExitCode { get; init; }

    /// <summary>The terminating signal.</summary>
    [JsonPropertyName("signal")]
    public string? Signal { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to wait for a terminal to exit.
/// </summary>
public sealed record AcpWaitForTerminalExitRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The terminal exit status.
/// </summary>
public sealed record AcpWaitForTerminalExitResponse
{
    /// <summary>The exit status.</summary>
    [JsonPropertyName("exitStatus")]
    public AcpTerminalExitStatus? ExitStatus { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to kill a terminal.
/// </summary>
public sealed record AcpKillTerminalRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of killing a terminal.
/// </summary>
public sealed record AcpKillTerminalResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to release a terminal.
/// </summary>
public sealed record AcpReleaseTerminalRequest
{
    /// <summary>The session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>The terminal identifier.</summary>
    [JsonPropertyName("terminalId")]
    public required string TerminalId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of releasing a terminal.
/// </summary>
public sealed record AcpReleaseTerminalResponse
{
    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A request to elicit structured input from the user.
/// </summary>
public sealed record AcpCreateElicitationRequest
{
    /// <summary>The message shown to the user.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>The elicitation mode.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }

    /// <summary>The requested form schema.</summary>
    [JsonPropertyName("requestedSchema")]
    public JsonElement? RequestedSchema { get; init; }

    /// <summary>The URL to open.</summary>
    [JsonPropertyName("url")]
    public string? Url { get; init; }

    /// <summary>The session scope.</summary>
    [JsonPropertyName("sessionId")]
    public string? SessionId { get; init; }

    /// <summary>The tool call scope.</summary>
    [JsonPropertyName("toolCallId")]
    public string? ToolCallId { get; init; }

    /// <summary>The request scope.</summary>
    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The user's elicitation response.
/// </summary>
public sealed record AcpCreateElicitationResponse
{
    /// <summary>The submitted content.</summary>
    [JsonPropertyName("content")]
    public JsonElement? Content { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A notification that an elicitation completed.
/// </summary>
public sealed record AcpCompleteElicitationNotification
{
    /// <summary>The elicitation identifier.</summary>
    [JsonPropertyName("elicitationId")]
    public required string ElicitationId { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}
