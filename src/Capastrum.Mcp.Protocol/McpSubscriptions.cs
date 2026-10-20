using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Selects the notifications delivered by a subscription stream.
/// </summary>
public sealed record McpSubscriptionFilter
{
    /// <summary>Receive tool list change notifications.</summary>
    [JsonPropertyName("toolsListChanged")]
    public bool? ToolsListChanged { get; init; }

    /// <summary>Receive prompt list change notifications.</summary>
    [JsonPropertyName("promptsListChanged")]
    public bool? PromptsListChanged { get; init; }

    /// <summary>Receive resource list change notifications.</summary>
    [JsonPropertyName("resourcesListChanged")]
    public bool? ResourcesListChanged { get; init; }

    /// <summary>Receive updates for these resource URIs.</summary>
    [JsonPropertyName("resourceSubscriptions")]
    public IReadOnlyList<string>? ResourceSubscriptions { get; init; }
}

/// <summary>
/// Parameters for <c>subscriptions/listen</c>.
/// </summary>
public sealed record McpSubscriptionsListenRequestParams : McpRequestParams
{
    /// <summary>The requested notification filters.</summary>
    [JsonPropertyName("notifications")]
    public required McpSubscriptionFilter Notifications { get; init; }
}

/// <summary>
/// The result that opens a subscription stream.
/// </summary>
public sealed record McpSubscriptionsListenResult
{
    /// <summary>The subscription metadata.</summary>
    [JsonPropertyName("_meta")]
    public required McpSubscriptionsListenResultMeta Meta { get; init; }

    /// <summary>The result type.</summary>
    [JsonPropertyName("resultType")]
    public string ResultType { get; init; } = McpProtocol.ResultTypes.Complete;
}

/// <summary>
/// Confirms the filters accepted by a subscription stream.
/// </summary>
public sealed record McpSubscriptionsAcknowledgedNotificationParams
{
    /// <summary>The accepted notification filters.</summary>
    [JsonPropertyName("notifications")]
    public required McpSubscriptionFilter Notifications { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}
