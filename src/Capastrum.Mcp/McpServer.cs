using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Capastrum.JsonRpc;
using Capastrum.JsonRpc.Serialization;
using Capastrum.JsonRpc.Transports;
using Capastrum.Mcp.Protocol;

namespace Capastrum.Mcp;

/// <summary>
/// A Model Context Protocol server. Registrations are shared by every session.
/// </summary>
public sealed class McpServer
{
    private static readonly JsonElement DefaultInputSchema = JsonSerializer.SerializeToElement(new { type = "object" });

    private readonly Dictionary<string, McpToolRegistration> _tools = new(StringComparer.Ordinal);
    private readonly Dictionary<string, McpResourceRegistration> _resources = new(StringComparer.Ordinal);
    private readonly List<McpResourceTemplateRegistration> _templates = [];
    private readonly Dictionary<string, McpPromptRegistration> _prompts = new(StringComparer.Ordinal);
    private readonly List<McpServerSession> _sessions = [];
    private readonly object _sync = new();
    private readonly McpServerOptions _options;
    private McpCompleteHandler? _completionHandler;

    /// <summary>
    /// Creates a server with the given options.
    /// </summary>
    public McpServer(McpServerOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(options.PageSize);

        _options = options;
    }

    /// <summary>The server options.</summary>
    public McpServerOptions Options => _options;

    /// <summary>The active sessions.</summary>
    public IReadOnlyCollection<McpServerSession> Sessions
    {
        get
        {
            lock (_sync)
            {
                return _sessions.ToArray();
            }
        }
    }

    /// <summary>Registers a tool.</summary>
    public McpServer RegisterTool(McpTool tool, McpToolHandler handler)
    {
        ArgumentNullException.ThrowIfNull(tool);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(tool.Name);

        _tools[tool.Name] = new McpToolRegistration(tool, handler);
        return this;
    }

    /// <summary>Registers a tool with a generated default input schema.</summary>
    public McpServer RegisterTool(
        string name,
        string? description,
        McpToolHandler handler,
        JsonElement? inputSchema = null,
        JsonElement? outputSchema = null,
        McpToolAnnotations? annotations = null) =>
        RegisterTool(
            new McpTool
            {
                Name = name,
                Description = description,
                InputSchema = inputSchema ?? DefaultInputSchema,
                OutputSchema = outputSchema,
                Annotations = annotations,
            },
            handler);

    /// <summary>Registers a resource with an exact URI.</summary>
    public McpServer RegisterResource(McpResource resource, McpResourceHandler handler)
    {
        ArgumentNullException.ThrowIfNull(resource);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(resource.Uri);

        _resources[resource.Uri] = new McpResourceRegistration(resource, handler);
        return this;
    }

    /// <summary>Registers a resource template.</summary>
    public McpServer RegisterResourceTemplate(McpResourceTemplate template, McpResourceHandler handler)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(template.UriTemplate);

        _templates.Add(new McpResourceTemplateRegistration(template, handler));
        return this;
    }

    /// <summary>Registers a prompt.</summary>
    public McpServer RegisterPrompt(McpPrompt prompt, McpPromptHandler handler)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(handler);
        ArgumentException.ThrowIfNullOrWhiteSpace(prompt.Name);

        _prompts[prompt.Name] = new McpPromptRegistration(prompt, handler);
        return this;
    }

    /// <summary>Registers the completion handler.</summary>
    public McpServer SetCompletionHandler(McpCompleteHandler handler)
    {
        ArgumentNullException.ThrowIfNull(handler);

        _completionHandler = handler;
        return this;
    }

    /// <summary>
    /// Creates a session that sends notifications through the given sender.
    /// </summary>
    public McpServerSession CreateSession(McpNotificationSender sender)
    {
        ArgumentNullException.ThrowIfNull(sender);

        var session = new McpServerSession(this, sender);
        lock (_sync)
        {
            _sessions.Add(session);
        }

        return session;
    }

    /// <summary>
    /// Runs a stdio session until the input stream closes or the token is cancelled.
    /// </summary>
    public async Task RunAsync(Stream input, Stream output, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(output);

        await using var transport = new NewlineJsonTransport(input, output);
        await using var peer = new JsonRpcPeer(transport);
        await using var session = CreateSession(
            (notification, token) => peer.NotifyAsync(notification.Method, notification.Params, token));

        peer.RequestHandler = session.HandleRequestAsync;
        peer.NotificationReceived += session.HandleNotification;

        try
        {
            await peer.Completion.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
    }

    /// <summary>
    /// Handles one stateless HTTP request. Subscriptions require a streaming transport.
    /// </summary>
    public async ValueTask<McpHttpResponse> HandleHttpRequestAsync(
        McpHttpRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (!string.Equals(request.Method, "POST", StringComparison.OrdinalIgnoreCase))
        {
            return new McpHttpResponse { StatusCode = 405 };
        }

        JsonRpcMessage? message;
        try
        {
            message = JsonSerializer.Deserialize(request.Body.Span, JsonRpcJsonContext.Default.JsonRpcMessage);
        }
        catch (JsonException exception)
        {
            return CreateErrorResponse(400, JsonRpcErrorCode.ParseError, exception.Message);
        }

        if (message is JsonRpcNotification)
        {
            return new McpHttpResponse { StatusCode = 202 };
        }

        if (message is not JsonRpcRequest rpcRequest)
        {
            return CreateErrorResponse(400, JsonRpcErrorCode.InvalidRequest, "A JSON-RPC request is required.");
        }

        if (!TryGetHeader(request.Headers, McpProtocol.Headers.Method, out var methodHeader))
        {
            return CreateErrorResponse(400, McpErrorCode.HeaderMismatch, "The Mcp-Method header is required.");
        }

        if (!string.Equals(methodHeader, rpcRequest.Method, StringComparison.Ordinal))
        {
            return CreateErrorResponse(400, McpErrorCode.HeaderMismatch, "The Mcp-Method header does not match the request.");
        }

        if (rpcRequest.Method is McpProtocol.Methods.CallTool or McpProtocol.Methods.GetPrompt)
        {
            if (!TryGetHeader(request.Headers, McpProtocol.Headers.Name, out var nameHeader))
            {
                return CreateErrorResponse(400, McpErrorCode.HeaderMismatch, "The Mcp-Name header is required.");
            }

            var requestedName = TryGetName(rpcRequest.Params);
            if (!string.Equals(nameHeader, requestedName, StringComparison.Ordinal))
            {
                return CreateErrorResponse(400, McpErrorCode.HeaderMismatch, "The Mcp-Name header does not match the request.");
            }
        }

        await using var session = CreateSession((_, _) => ValueTask.CompletedTask);
        var response = await session.HandleRequestAsync(rpcRequest, cancellationToken).ConfigureAwait(false);
        var body = JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(response, JsonRpcJsonContext.Default.JsonRpcMessage);

        return new McpHttpResponse
        {
            StatusCode = response.Error?.Code is McpErrorCode.HeaderMismatch
                or McpErrorCode.MissingRequiredClientCapability
                or McpErrorCode.UnsupportedProtocolVersion
                ? 400
                : 200,
            ContentType = "application/json",
            Body = body,
        };
    }

    /// <summary>Notifies every subscription that the tool list changed.</summary>
    public async ValueTask NotifyToolsChangedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var session in SnapshotSessions())
        {
            await SendToSessionAsync(session, static (s, ct) => s.NotifyToolsChangedAsync(ct), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Notifies every subscription that the resource list changed.</summary>
    public async ValueTask NotifyResourcesChangedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var session in SnapshotSessions())
        {
            await SendToSessionAsync(session, static (s, ct) => s.NotifyResourcesChangedAsync(ct), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Notifies every subscription that the prompt list changed.</summary>
    public async ValueTask NotifyPromptsChangedAsync(CancellationToken cancellationToken = default)
    {
        foreach (var session in SnapshotSessions())
        {
            await SendToSessionAsync(session, static (s, ct) => s.NotifyPromptsChangedAsync(ct), cancellationToken)
                .ConfigureAwait(false);
        }
    }

    /// <summary>Notifies subscribers of the given resource that it changed.</summary>
    public async ValueTask NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        foreach (var session in SnapshotSessions())
        {
            await SendToSessionAsync(
                session,
                (s, ct) => s.NotifyResourceUpdatedAsync(uri, ct),
                cancellationToken).ConfigureAwait(false);
        }
    }

    internal IReadOnlyDictionary<string, McpToolRegistration> Tools => _tools;

    internal IReadOnlyDictionary<string, McpResourceRegistration> Resources => _resources;

    internal IReadOnlyList<McpResourceTemplateRegistration> Templates => _templates;

    internal IReadOnlyDictionary<string, McpPromptRegistration> Prompts => _prompts;

    internal McpCompleteHandler? CompletionHandler => _completionHandler;

    internal McpServerCapabilities CreateCapabilities() => new()
    {
        Tools = _tools.Count > 0 ? JsonSerializer.SerializeToElement(new { listChanged = true }) : null,
        Resources = _resources.Count + _templates.Count > 0
            ? JsonSerializer.SerializeToElement(new { listChanged = true, subscribe = _options.EnableSubscriptions })
            : null,
        Prompts = _prompts.Count > 0 ? JsonSerializer.SerializeToElement(new { listChanged = true }) : null,
        Completions = _completionHandler is not null ? JsonSerializer.SerializeToElement(new { }) : null,
        Logging = _options.EnableLogging ? JsonSerializer.SerializeToElement(new { }) : null,
    };

    internal void RemoveSession(McpServerSession session)
    {
        lock (_sync)
        {
            _sessions.Remove(session);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "A disconnected session must not stop notifications to the remaining sessions.")]
    private async ValueTask SendToSessionAsync(
        McpServerSession session,
        Func<McpServerSession, CancellationToken, ValueTask> send,
        CancellationToken cancellationToken)
    {
        try
        {
            await send(session, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception)
        {
            RemoveSession(session);
        }
    }

    private McpServerSession[] SnapshotSessions()
    {
        lock (_sync)
        {
            return _sessions.ToArray();
        }
    }

    private static bool TryGetHeader(
        IReadOnlyDictionary<string, string> headers,
        string name,
        [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out string? value)
    {
        foreach (var header in headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                value = header.Value;
                return true;
            }
        }

        value = null;
        return false;
    }

    private static string? TryGetName(JsonElement? parameters)
    {
        if (parameters is not { ValueKind: JsonValueKind.Object } element)
        {
            return null;
        }

        return element.TryGetProperty("name", out var name) && name.ValueKind == JsonValueKind.String
            ? name.GetString()
            : null;
    }

    private static McpHttpResponse CreateErrorResponse(int statusCode, int code, string message)
    {
        var response = new JsonRpcResponse
        {
            Id = null,
            Error = new JsonRpcError { Code = code, Message = message },
        };

        return new McpHttpResponse
        {
            StatusCode = statusCode,
            ContentType = "application/json",
            Body = JsonSerializer.SerializeToUtf8Bytes<JsonRpcMessage>(response, JsonRpcJsonContext.Default.JsonRpcMessage),
        };
    }
}

/// <summary>Handles a tool call.</summary>
public delegate ValueTask<McpResult> McpToolHandler(McpToolCallContext context, CancellationToken cancellationToken);

/// <summary>Handles a resource read.</summary>
public delegate ValueTask<McpReadResourceResult> McpResourceHandler(
    McpReadResourceContext context,
    CancellationToken cancellationToken);

/// <summary>Handles a prompt request.</summary>
public delegate ValueTask<McpGetPromptResult> McpPromptHandler(
    McpGetPromptContext context,
    CancellationToken cancellationToken);

/// <summary>Handles a completion request.</summary>
public delegate ValueTask<McpCompleteResult> McpCompleteHandler(
    McpCompleteRequestParams parameters,
    CancellationToken cancellationToken);

/// <summary>Sends a notification to a session peer.</summary>
public delegate ValueTask McpNotificationSender(JsonRpcNotification notification, CancellationToken cancellationToken);

internal sealed record McpToolRegistration(McpTool Tool, McpToolHandler Handler);

internal sealed record McpResourceRegistration(McpResource Resource, McpResourceHandler Handler);

internal sealed record McpResourceTemplateRegistration(McpResourceTemplate Template, McpResourceHandler Handler);

internal sealed record McpPromptRegistration(McpPrompt Prompt, McpPromptHandler Handler);
