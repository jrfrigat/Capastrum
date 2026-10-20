using System.Text.Json;
using System.Text.Json.Serialization;
using Capastrum.JsonRpc;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// Parameters for the cancellation notification.
/// </summary>
public sealed record McpCancelledNotificationParams
{
    /// <summary>The id of the request to cancel.</summary>
    [JsonPropertyName("requestId")]
    public required JsonRpcId RequestId { get; init; }

    /// <summary>An optional reason.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Parameters for the progress notification.
/// </summary>
public sealed record McpProgressNotificationParams
{
    /// <summary>The progress token from the request.</summary>
    [JsonPropertyName("progressToken")]
    public required JsonRpcId ProgressToken { get; init; }

    /// <summary>The current progress value.</summary>
    [JsonPropertyName("progress")]
    public required double Progress { get; init; }

    /// <summary>The total progress value, when known.</summary>
    [JsonPropertyName("total")]
    public double? Total { get; init; }

    /// <summary>An optional progress message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Parameters for the logging message notification.
/// </summary>
public sealed record McpLoggingMessageNotificationParams
{
    /// <summary>The log level.</summary>
    [JsonPropertyName("level")]
    public required McpLoggingLevel Level { get; init; }

    /// <summary>The logger name.</summary>
    [JsonPropertyName("logger")]
    public string? Logger { get; init; }

    /// <summary>The logged data.</summary>
    [JsonPropertyName("data")]
    public required JsonElement Data { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Parameters for the resource updated notification.
/// </summary>
public sealed record McpResourceUpdatedNotificationParams
{
    /// <summary>The updated resource URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}
