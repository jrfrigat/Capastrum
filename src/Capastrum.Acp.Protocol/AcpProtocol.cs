using System.Text.Json.Serialization;

namespace Capastrum.Acp.Protocol;

/// <summary>
/// Constants for the Agent Client Protocol version 1.
/// </summary>
public static class AcpProtocol
{
    /// <summary>The protocol version implemented by this package.</summary>
    public const int Version = 1;

    /// <summary>The JSON-RPC version used on the wire.</summary>
    public const string JsonRpcVersion = "2.0";

    /// <summary>Request method names.</summary>
    public static class Methods
    {
        /// <summary>Initializes the connection.</summary>
        public const string Initialize = "initialize";

        /// <summary>Authenticates the client.</summary>
        public const string Authenticate = "authenticate";

        /// <summary>Logs out.</summary>
        public const string Logout = "logout";

        /// <summary>Creates a session.</summary>
        public const string SessionNew = "session/new";

        /// <summary>Loads a session.</summary>
        public const string SessionLoad = "session/load";

        /// <summary>Resumes a session.</summary>
        public const string SessionResume = "session/resume";

        /// <summary>Lists sessions.</summary>
        public const string SessionList = "session/list";

        /// <summary>Closes a session.</summary>
        public const string SessionClose = "session/close";

        /// <summary>Deletes a session.</summary>
        public const string SessionDelete = "session/delete";

        /// <summary>Sets the session mode.</summary>
        public const string SessionSetMode = "session/set_mode";

        /// <summary>Sets a session configuration option.</summary>
        public const string SessionSetConfigOption = "session/set_config_option";

        /// <summary>Processes a prompt turn.</summary>
        public const string SessionPrompt = "session/prompt";

        /// <summary>Requests permission for a tool call.</summary>
        public const string SessionRequestPermission = "session/request_permission";

        /// <summary>Reads a text file.</summary>
        public const string FsReadTextFile = "fs/read_text_file";

        /// <summary>Writes a text file.</summary>
        public const string FsWriteTextFile = "fs/write_text_file";

        /// <summary>Creates a terminal.</summary>
        public const string TerminalCreate = "terminal/create";

        /// <summary>Reads terminal output.</summary>
        public const string TerminalOutput = "terminal/output";

        /// <summary>Waits for a terminal to exit.</summary>
        public const string TerminalWaitForExit = "terminal/wait_for_exit";

        /// <summary>Kills a terminal.</summary>
        public const string TerminalKill = "terminal/kill";

        /// <summary>Releases a terminal.</summary>
        public const string TerminalRelease = "terminal/release";

        /// <summary>Requests structured user input.</summary>
        public const string ElicitationCreate = "elicitation/create";
    }

    /// <summary>Notification method names.</summary>
    public static class Notifications
    {
        /// <summary>Streams a session update.</summary>
        public const string SessionUpdate = "session/update";

        /// <summary>Cancels the current prompt turn.</summary>
        public const string SessionCancel = "session/cancel";

        /// <summary>Completes an elicitation.</summary>
        public const string ElicitationComplete = "elicitation/complete";

        /// <summary>Cancels a specific JSON-RPC request.</summary>
        public const string CancelRequest = "$/cancel_request";
    }
}

/// <summary>
/// Reasons why an agent stops processing a prompt turn.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpStopReason>))]
public enum AcpStopReason
{
    /// <summary>The turn ended successfully.</summary>
    [JsonStringEnumMemberName("end_turn")]
    EndTurn,

    /// <summary>The token limit was reached.</summary>
    [JsonStringEnumMemberName("max_tokens")]
    MaxTokens,

    /// <summary>The request limit between user turns was reached.</summary>
    [JsonStringEnumMemberName("max_turn_requests")]
    MaxTurnRequests,

    /// <summary>The agent refused to continue.</summary>
    [JsonStringEnumMemberName("refusal")]
    Refusal,

    /// <summary>The client cancelled the turn.</summary>
    [JsonStringEnumMemberName("cancelled")]
    Cancelled,
}

/// <summary>
/// Kinds of work a tool call performs.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpToolKind>))]
public enum AcpToolKind
{
    /// <summary>Reads data.</summary>
    [JsonStringEnumMemberName("read")]
    Read,

    /// <summary>Edits data.</summary>
    [JsonStringEnumMemberName("edit")]
    Edit,

    /// <summary>Deletes data.</summary>
    [JsonStringEnumMemberName("delete")]
    Delete,

    /// <summary>Moves data.</summary>
    [JsonStringEnumMemberName("move")]
    Move,

    /// <summary>Searches data.</summary>
    [JsonStringEnumMemberName("search")]
    Search,

    /// <summary>Executes a command.</summary>
    [JsonStringEnumMemberName("execute")]
    Execute,

    /// <summary>Reasons internally.</summary>
    [JsonStringEnumMemberName("think")]
    Think,

    /// <summary>Fetches remote data.</summary>
    [JsonStringEnumMemberName("fetch")]
    Fetch,

    /// <summary>Switches the session mode.</summary>
    [JsonStringEnumMemberName("switch_mode")]
    SwitchMode,

    /// <summary>Another kind.</summary>
    [JsonStringEnumMemberName("other")]
    Other,
}

/// <summary>
/// Lifecycle state of a tool call.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpToolCallStatus>))]
public enum AcpToolCallStatus
{
    /// <summary>The call is pending.</summary>
    [JsonStringEnumMemberName("pending")]
    Pending,

    /// <summary>The call is running.</summary>
    [JsonStringEnumMemberName("in_progress")]
    InProgress,

    /// <summary>The call completed.</summary>
    [JsonStringEnumMemberName("completed")]
    Completed,

    /// <summary>The call failed.</summary>
    [JsonStringEnumMemberName("failed")]
    Failed,
}

/// <summary>
/// Kinds of permission options.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpPermissionOptionKind>))]
public enum AcpPermissionOptionKind
{
    /// <summary>Allow once.</summary>
    [JsonStringEnumMemberName("allow_once")]
    AllowOnce,

    /// <summary>Allow always.</summary>
    [JsonStringEnumMemberName("allow_always")]
    AllowAlways,

    /// <summary>Reject once.</summary>
    [JsonStringEnumMemberName("reject_once")]
    RejectOnce,

    /// <summary>Reject always.</summary>
    [JsonStringEnumMemberName("reject_always")]
    RejectAlways,
}

/// <summary>
/// Plan entry priority.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpPlanEntryPriority>))]
public enum AcpPlanEntryPriority
{
    /// <summary>High priority.</summary>
    [JsonStringEnumMemberName("high")]
    High,

    /// <summary>Medium priority.</summary>
    [JsonStringEnumMemberName("medium")]
    Medium,

    /// <summary>Low priority.</summary>
    [JsonStringEnumMemberName("low")]
    Low,
}

/// <summary>
/// Plan entry status.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<AcpPlanEntryStatus>))]
public enum AcpPlanEntryStatus
{
    /// <summary>Pending.</summary>
    [JsonStringEnumMemberName("pending")]
    Pending,

    /// <summary>In progress.</summary>
    [JsonStringEnumMemberName("in_progress")]
    InProgress,

    /// <summary>Completed.</summary>
    [JsonStringEnumMemberName("completed")]
    Completed,
}
