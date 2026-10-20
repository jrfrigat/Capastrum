namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Constants for the Model Context Protocol version 2026-07-28.
/// </summary>
public static class McpProtocol
{
    /// <summary>
    /// The protocol revision implemented by this package.
    /// </summary>
    public const string Version = "2026-07-28";

    /// <summary>
    /// The JSON-RPC version used on the wire.
    /// </summary>
    public const string JsonRpcVersion = "2.0";

    /// <summary>
    /// Request method names.
    /// </summary>
    public static class Methods
    {
        /// <summary>Discovers server identity, capabilities and supported versions.</summary>
        public const string Discover = "server/discover";

        /// <summary>Lists tools.</summary>
        public const string ListTools = "tools/list";

        /// <summary>Calls a tool.</summary>
        public const string CallTool = "tools/call";

        /// <summary>Lists resources.</summary>
        public const string ListResources = "resources/list";

        /// <summary>Reads a resource.</summary>
        public const string ReadResource = "resources/read";

        /// <summary>Lists resource templates.</summary>
        public const string ListResourceTemplates = "resources/templates/list";

        /// <summary>Lists prompts.</summary>
        public const string ListPrompts = "prompts/list";

        /// <summary>Gets a prompt.</summary>
        public const string GetPrompt = "prompts/get";

        /// <summary>Requests argument completion.</summary>
        public const string Complete = "completion/complete";

        /// <summary>Opens a subscription stream.</summary>
        public const string Listen = "subscriptions/listen";

        /// <summary>Samples from a model on the client.</summary>
        public const string SamplingCreateMessage = "sampling/createMessage";

        /// <summary>Lists client roots.</summary>
        public const string ListRoots = "roots/list";

        /// <summary>Requests structured input from the user.</summary>
        public const string Elicit = "elicitation/create";
    }

    /// <summary>
    /// Notification method names.
    /// </summary>
    public static class Notifications
    {
        /// <summary>Cancels an in-flight request.</summary>
        public const string Cancelled = "notifications/cancelled";

        /// <summary>Reports progress for a request.</summary>
        public const string Progress = "notifications/progress";

        /// <summary>Sends a log message.</summary>
        public const string Message = "notifications/message";

        /// <summary>Reports that the tool list changed.</summary>
        public const string ToolsListChanged = "notifications/tools/list_changed";

        /// <summary>Reports that the resource list changed.</summary>
        public const string ResourcesListChanged = "notifications/resources/list_changed";

        /// <summary>Reports that a resource changed.</summary>
        public const string ResourceUpdated = "notifications/resources/updated";

        /// <summary>Reports that the prompt list changed.</summary>
        public const string PromptsListChanged = "notifications/prompts/list_changed";

        /// <summary>Confirms the filters accepted by a subscription stream.</summary>
        public const string SubscriptionsAcknowledged = "notifications/subscriptions/acknowledged";
    }

    /// <summary>
    /// Values of the <c>resultType</c> field.
    /// </summary>
    public static class ResultTypes
    {
        /// <summary>The request completed normally.</summary>
        public const string Complete = "complete";

        /// <summary>The request needs input from the caller before it can continue.</summary>
        public const string InputRequired = "input_required";
    }

    /// <summary>
    /// HTTP headers used by the Streamable HTTP transport.
    /// </summary>
    public static class Headers
    {
        /// <summary>The request method.</summary>
        public const string Method = "Mcp-Method";

        /// <summary>The tool or prompt name, when the method addresses one.</summary>
        public const string Name = "Mcp-Name";
    }

    /// <summary>
    /// Reserved keys under <c>_meta</c>.
    /// </summary>
    public static class MetaKeys
    {
        /// <summary>The protocol version carried by every request.</summary>
        public const string ProtocolVersion = "io.modelcontextprotocol/protocolVersion";

        /// <summary>The client capabilities carried by every request.</summary>
        public const string ClientCapabilities = "io.modelcontextprotocol/clientCapabilities";

        /// <summary>The client implementation carried by a request.</summary>
        public const string ClientInfo = "io.modelcontextprotocol/clientInfo";

        /// <summary>The server implementation carried by a response.</summary>
        public const string ServerInfo = "io.modelcontextprotocol/serverInfo";

        /// <summary>The requested log level.</summary>
        public const string LogLevel = "io.modelcontextprotocol/logLevel";

        /// <summary>The subscription stream identifier.</summary>
        public const string SubscriptionId = "io.modelcontextprotocol/subscriptionId";
    }
}
