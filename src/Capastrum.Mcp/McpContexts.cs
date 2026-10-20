using System.Text.Json;
using Capastrum.Mcp.Protocol;

namespace Capastrum.Mcp;

/// <summary>
/// The context passed to a tool handler.
/// </summary>
public sealed class McpToolCallContext
{
    private readonly McpServerSession _session;

    internal McpToolCallContext(McpServerSession session, McpCallToolRequestParams parameters)
    {
        _session = session;
        Name = parameters.Name;
        Arguments = parameters.Arguments ?? default;
        Meta = parameters.Meta;
        InputResponses = parameters.InputResponses;
        RequestState = parameters.RequestState;
    }

    /// <summary>The tool name.</summary>
    public string Name { get; }

    /// <summary>The tool arguments.</summary>
    public JsonElement Arguments { get; }

    /// <summary>The request metadata.</summary>
    public McpRequestMeta Meta { get; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    public IReadOnlyDictionary<string, JsonElement>? InputResponses { get; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    public string? RequestState { get; }

    /// <summary>Reports progress for the current request.</summary>
    public ValueTask ReportProgressAsync(
        double progress,
        double? total = null,
        string? message = null,
        CancellationToken cancellationToken = default) =>
        _session.SendProgressAsync(Meta.ProgressToken, progress, total, message, cancellationToken);

    /// <summary>Sends a log message to the client.</summary>
    public ValueTask LogAsync(
        McpLoggingLevel level,
        JsonElement data,
        string? logger = null,
        CancellationToken cancellationToken = default) =>
        _session.SendLogAsync(level, data, logger, cancellationToken);

    /// <summary>Sends a text log message to the client.</summary>
    public ValueTask LogAsync(
        McpLoggingLevel level,
        string message,
        string? logger = null,
        CancellationToken cancellationToken = default) =>
        _session.SendLogAsync(level, JsonSerializer.SerializeToElement(message), logger, cancellationToken);
}

/// <summary>
/// The context passed to a resource handler.
/// </summary>
public sealed class McpReadResourceContext
{
    internal McpReadResourceContext(McpReadResourceRequestParams parameters)
    {
        Uri = parameters.Uri;
        Meta = parameters.Meta;
        InputResponses = parameters.InputResponses;
        RequestState = parameters.RequestState;
    }

    /// <summary>The resource URI.</summary>
    public string Uri { get; }

    /// <summary>The request metadata.</summary>
    public McpRequestMeta Meta { get; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    public IReadOnlyDictionary<string, JsonElement>? InputResponses { get; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    public string? RequestState { get; }
}

/// <summary>
/// The context passed to a prompt handler.
/// </summary>
public sealed class McpGetPromptContext
{
    internal McpGetPromptContext(McpGetPromptRequestParams parameters)
    {
        Name = parameters.Name;
        Arguments = parameters.Arguments;
        Meta = parameters.Meta;
        InputResponses = parameters.InputResponses;
        RequestState = parameters.RequestState;
    }

    /// <summary>The prompt name.</summary>
    public string Name { get; }

    /// <summary>The prompt arguments.</summary>
    public IReadOnlyDictionary<string, string>? Arguments { get; }

    /// <summary>The request metadata.</summary>
    public McpRequestMeta Meta { get; }

    /// <summary>Responses to input requests from a previous round trip.</summary>
    public IReadOnlyDictionary<string, JsonElement>? InputResponses { get; }

    /// <summary>Opaque state returned by a previous round trip.</summary>
    public string? RequestState { get; }
}
