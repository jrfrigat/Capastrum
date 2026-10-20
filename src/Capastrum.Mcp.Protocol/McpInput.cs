using System.Text.Json;
using System.Text.Json.Serialization;

namespace Capastrum.Mcp.Protocol;

/// <summary>
/// A result that asks the caller to provide input and retry the request.
/// </summary>
public sealed record McpInputRequiredResult : McpResult
{
    /// <summary>The requests keyed by input id.</summary>
    [JsonPropertyName("inputRequests")]
    public Dictionary<string, McpInputRequest>? InputRequests { get; init; }

    /// <summary>Opaque state the caller must send back with the retry.</summary>
    [JsonPropertyName("requestState")]
    public string? RequestState { get; init; }

    /// <summary>The result type.</summary>
    [JsonPropertyName("resultType")]
    public override string ResultType => McpProtocol.ResultTypes.InputRequired;
}

/// <summary>
/// An input request returned inside an <see cref="McpInputRequiredResult"/>.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "method")]
[JsonDerivedType(typeof(McpCreateMessageInputRequest), "sampling/createMessage")]
[JsonDerivedType(typeof(McpListRootsInputRequest), "roots/list")]
[JsonDerivedType(typeof(McpElicitInputRequest), "elicitation/create")]
public abstract record McpInputRequest;

/// <summary>
/// A sampling request.
/// </summary>
public sealed record McpCreateMessageInputRequest : McpInputRequest
{
    /// <summary>The sampling parameters.</summary>
    [JsonPropertyName("params")]
    public required McpCreateMessageRequestParams Params { get; init; }
}

/// <summary>
/// A request for the client roots.
/// </summary>
public sealed record McpListRootsInputRequest : McpInputRequest
{
    /// <summary>The request parameters.</summary>
    [JsonPropertyName("params")]
    public JsonElement? Params { get; init; }
}

/// <summary>
/// An elicitation request.
/// </summary>
public sealed record McpElicitInputRequest : McpInputRequest
{
    /// <summary>The elicitation parameters.</summary>
    [JsonPropertyName("params")]
    public required McpElicitRequestParams Params { get; init; }
}

/// <summary>
/// Parameters for a sampling request.
/// </summary>
public sealed record McpCreateMessageRequestParams
{
    /// <summary>The conversation messages.</summary>
    [JsonPropertyName("messages")]
    public required IReadOnlyList<McpSamplingMessage> Messages { get; init; }

    /// <summary>The maximum number of tokens to sample.</summary>
    [JsonPropertyName("maxTokens")]
    public required int MaxTokens { get; init; }

    /// <summary>An optional system prompt.</summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; init; }

    /// <summary>Whether to include context from MCP servers.</summary>
    [JsonPropertyName("includeContext")]
    public string? IncludeContext { get; init; }

    /// <summary>The sampling temperature.</summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; init; }

    /// <summary>Sequences that stop generation.</summary>
    [JsonPropertyName("stopSequences")]
    public IReadOnlyList<string>? StopSequences { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("metadata")]
    public JsonElement? Metadata { get; init; }

    /// <summary>Model selection preferences.</summary>
    [JsonPropertyName("modelPreferences")]
    public McpModelPreferences? ModelPreferences { get; init; }

    /// <summary>Tools the model may call.</summary>
    [JsonPropertyName("tools")]
    public IReadOnlyList<McpTool>? Tools { get; init; }

    /// <summary>Tool selection preference.</summary>
    [JsonPropertyName("toolChoice")]
    public McpToolChoice? ToolChoice { get; init; }
}

/// <summary>
/// A message in a sampling request.
/// </summary>
public sealed record McpSamplingMessage
{
    /// <summary>The message author role.</summary>
    [JsonPropertyName("role")]
    public required McpRole Role { get; init; }

    /// <summary>The message content, a single block or an array of blocks.</summary>
    [JsonPropertyName("content")]
    public required JsonElement Content { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// Model selection preferences.
/// </summary>
public sealed record McpModelPreferences
{
    /// <summary>Preference for lower cost.</summary>
    [JsonPropertyName("costPriority")]
    public double? CostPriority { get; init; }

    /// <summary>Preference for lower latency.</summary>
    [JsonPropertyName("speedPriority")]
    public double? SpeedPriority { get; init; }

    /// <summary>Preference for higher intelligence.</summary>
    [JsonPropertyName("intelligencePriority")]
    public double? IntelligencePriority { get; init; }

    /// <summary>Model hints.</summary>
    [JsonPropertyName("hints")]
    public IReadOnlyList<McpModelHint>? Hints { get; init; }
}

/// <summary>
/// A model hint.
/// </summary>
public sealed record McpModelHint
{
    /// <summary>The model name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }
}

/// <summary>
/// A tool selection preference.
/// </summary>
public sealed record McpToolChoice
{
    /// <summary>The selection mode.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; init; }
}

/// <summary>
/// The result of a sampling request.
/// </summary>
public sealed record McpCreateMessageResult
{
    /// <summary>The sampled content.</summary>
    [JsonPropertyName("content")]
    public required JsonElement Content { get; init; }

    /// <summary>The model that produced the result.</summary>
    [JsonPropertyName("model")]
    public required string Model { get; init; }

    /// <summary>The result role.</summary>
    [JsonPropertyName("role")]
    public required McpRole Role { get; init; }

    /// <summary>The reason generation stopped.</summary>
    [JsonPropertyName("stopReason")]
    public string? StopReason { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// A client root.
/// </summary>
public sealed record McpRoot
{
    /// <summary>The root URI.</summary>
    [JsonPropertyName("uri")]
    public required string Uri { get; init; }

    /// <summary>A human-readable name.</summary>
    [JsonPropertyName("name")]
    public string? Name { get; init; }

    /// <summary>Implementation-specific metadata.</summary>
    [JsonPropertyName("_meta")]
    public Dictionary<string, JsonElement>? Meta { get; init; }
}

/// <summary>
/// The result of a roots request.
/// </summary>
public sealed record McpListRootsResult
{
    /// <summary>The client roots.</summary>
    [JsonPropertyName("roots")]
    public required IReadOnlyList<McpRoot> Roots { get; init; }
}

/// <summary>
/// Parameters for an elicitation request.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "mode")]
[JsonDerivedType(typeof(McpElicitFormRequestParams), "form")]
[JsonDerivedType(typeof(McpElicitUrlRequestParams), "url")]
public abstract record McpElicitRequestParams;

/// <summary>
/// An elicitation form.
/// </summary>
public sealed record McpElicitFormRequestParams : McpElicitRequestParams
{
    /// <summary>The message shown to the user.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>The JSON Schema of the requested fields.</summary>
    [JsonPropertyName("requestedSchema")]
    public required JsonElement RequestedSchema { get; init; }
}

/// <summary>
/// An elicitation that redirects the user to a URL.
/// </summary>
public sealed record McpElicitUrlRequestParams : McpElicitRequestParams
{
    /// <summary>The message shown to the user.</summary>
    [JsonPropertyName("message")]
    public required string Message { get; init; }

    /// <summary>The URL to open.</summary>
    [JsonPropertyName("url")]
    public required string Url { get; init; }
}

/// <summary>
/// The result of an elicitation request.
/// </summary>
public sealed record McpElicitResult
{
    /// <summary>The user's action.</summary>
    [JsonPropertyName("action")]
    public required McpElicitAction Action { get; init; }

    /// <summary>The submitted content.</summary>
    [JsonPropertyName("content")]
    public JsonElement? Content { get; init; }
}

/// <summary>
/// The action taken by the user in an elicitation.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<McpElicitAction>))]
public enum McpElicitAction
{
    /// <summary>The user accepted the request.</summary>
    [JsonStringEnumMemberName("accept")]
    Accept,

    /// <summary>The user declined the request.</summary>
    [JsonStringEnumMemberName("decline")]
    Decline,

    /// <summary>The user cancelled the request.</summary>
    [JsonStringEnumMemberName("cancel")]
    Cancel,
}
