using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using Capastrum.JsonRpc;
using Capastrum.Mcp.Protocol;
using Capastrum.Mcp.Protocol.Serialization;

namespace Capastrum.Mcp;

/// <summary>
/// Handles one MCP connection. A session holds the subscription streams of its peer.
/// </summary>
public sealed class McpServerSession : IAsyncDisposable
{
    private readonly McpServer _server;
    private readonly McpNotificationSender _sender;
    private readonly ConcurrentDictionary<JsonRpcId, McpSubscriptionFilter> _subscriptions = new();
    private bool _disposed;

    internal McpServerSession(McpServer server, McpNotificationSender sender)
    {
        _server = server;
        _sender = sender;
    }

    /// <summary>
    /// Handles one JSON-RPC request.
    /// </summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "An unexpected handler failure must be returned as a JSON-RPC internal error.")]
    public async ValueTask<JsonRpcResponse> HandleRequestAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            return await DispatchAsync(request, cancellationToken).ConfigureAwait(false);
        }
        catch (JsonRpcException exception)
        {
            return Error(request.Id, exception.Code, exception.Message, exception.Data);
        }
        catch (Exception exception)
        {
            return Error(request.Id, JsonRpcErrorCode.InternalError, exception.Message, data: null);
        }
    }

    /// <summary>
    /// Handles a notification received by the peer.
    /// </summary>
    public void HandleNotification(JsonRpcNotification notification)
    {
        ArgumentNullException.ThrowIfNull(notification);

        if (notification.Method != McpProtocol.Notifications.Cancelled || notification.Params is not { } parameters)
        {
            return;
        }

        var cancelled = parameters.Deserialize(McpJsonContext.Default.McpCancelledNotificationParams);
        if (cancelled is not null)
        {
            _subscriptions.TryRemove(cancelled.RequestId, out _);
        }
    }

    /// <summary>Notifies subscriptions that the tool list changed.</summary>
    public ValueTask NotifyToolsChangedAsync(CancellationToken cancellationToken = default) =>
        NotifySubscriptionsAsync(
            static filter => filter.ToolsListChanged == true,
            McpProtocol.Notifications.ToolsListChanged,
            static subscriptionId => JsonSerializer.SerializeToElement(
                new { _meta = CreateSubscriptionMeta(subscriptionId) }),
            cancellationToken);

    /// <summary>Notifies subscriptions that the resource list changed.</summary>
    public ValueTask NotifyResourcesChangedAsync(CancellationToken cancellationToken = default) =>
        NotifySubscriptionsAsync(
            static filter => filter.ResourcesListChanged == true,
            McpProtocol.Notifications.ResourcesListChanged,
            static subscriptionId => JsonSerializer.SerializeToElement(
                new { _meta = CreateSubscriptionMeta(subscriptionId) }),
            cancellationToken);

    /// <summary>Notifies subscriptions that the prompt list changed.</summary>
    public ValueTask NotifyPromptsChangedAsync(CancellationToken cancellationToken = default) =>
        NotifySubscriptionsAsync(
            static filter => filter.PromptsListChanged == true,
            McpProtocol.Notifications.PromptsListChanged,
            static subscriptionId => JsonSerializer.SerializeToElement(
                new { _meta = CreateSubscriptionMeta(subscriptionId) }),
            cancellationToken);

    /// <summary>Notifies subscriptions that a resource changed.</summary>
    public ValueTask NotifyResourceUpdatedAsync(string uri, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        return NotifySubscriptionsAsync(
            filter => filter.ResourceSubscriptions?.Contains(uri, StringComparer.Ordinal) == true,
            McpProtocol.Notifications.ResourceUpdated,
            subscriptionId => JsonSerializer.SerializeToElement(
                new McpResourceUpdatedNotificationParams
                {
                    Uri = uri,
                    Meta = CreateSubscriptionMeta(subscriptionId),
                },
                McpJsonContext.Default.McpResourceUpdatedNotificationParams),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return ValueTask.CompletedTask;
        }

        _disposed = true;
        _server.RemoveSession(this);
        return ValueTask.CompletedTask;
    }

    internal ValueTask SendProgressAsync(
        JsonRpcId? progressToken,
        double progress,
        double? total,
        string? message,
        CancellationToken cancellationToken)
    {
        if (progressToken is null)
        {
            return ValueTask.CompletedTask;
        }

        var parameters = new McpProgressNotificationParams
        {
            ProgressToken = progressToken.Value,
            Progress = progress,
            Total = total,
            Message = message,
        };

        return SendAsync(
            McpProtocol.Notifications.Progress,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpProgressNotificationParams),
            cancellationToken);
    }

    internal ValueTask SendLogAsync(
        McpLoggingLevel level,
        JsonElement data,
        string? logger,
        CancellationToken cancellationToken)
    {
        if (!_server.Options.EnableLogging)
        {
            return ValueTask.CompletedTask;
        }

        var parameters = new McpLoggingMessageNotificationParams
        {
            Level = level,
            Logger = logger,
            Data = data,
        };

        return SendAsync(
            McpProtocol.Notifications.Message,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpLoggingMessageNotificationParams),
            cancellationToken);
    }

    private async ValueTask<JsonRpcResponse> DispatchAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken) =>
        request.Method switch
        {
            McpProtocol.Methods.Discover => Success(
                request.Id,
                await HandleDiscoverAsync(request, cancellationToken).ConfigureAwait(false)),

            McpProtocol.Methods.ListTools => Success(
                request.Id,
                HandleListTools(request)),

            McpProtocol.Methods.CallTool => Success(
                request.Id,
                await HandleCallToolAsync(request, cancellationToken).ConfigureAwait(false)),

            McpProtocol.Methods.ListResources => Success(
                request.Id,
                HandleListResources(request)),

            McpProtocol.Methods.ReadResource => Success(
                request.Id,
                await HandleReadResourceAsync(request, cancellationToken).ConfigureAwait(false)),

            McpProtocol.Methods.ListResourceTemplates => Success(
                request.Id,
                HandleListResourceTemplates(request)),

            McpProtocol.Methods.ListPrompts => Success(
                request.Id,
                HandleListPrompts(request)),

            McpProtocol.Methods.GetPrompt => Success(
                request.Id,
                await HandleGetPromptAsync(request, cancellationToken).ConfigureAwait(false)),

            McpProtocol.Methods.Complete => Success(
                request.Id,
                await HandleCompleteAsync(request, cancellationToken).ConfigureAwait(false)),

            McpProtocol.Methods.Listen => SuccessElement(
                request.Id,
                await HandleListenAsync(request, cancellationToken).ConfigureAwait(false)),

            _ => throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                $"Method '{request.Method}' is not supported."),
        };

    private ValueTask<McpDiscoverResult> HandleDiscoverAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        _ = cancellationToken;
        var parameters = Deserialize(request, McpJsonContext.Default.McpDiscoverRequestParams);
        ValidateMeta(parameters.Meta);

        return ValueTask.FromResult(new McpDiscoverResult
        {
            Meta = new McpResultMeta { ServerInfo = CreateServerInfo() },
            SupportedVersions = new[] { McpProtocol.Version },
            Capabilities = _server.CreateCapabilities(),
            Instructions = _server.Options.Instructions,
            TtlMs = 0,
            CacheScope = _server.Options.DiscoverCacheScope,
        });
    }

    private McpListToolsResult HandleListTools(JsonRpcRequest request)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpListToolsRequestParams);
        ValidateMeta(parameters.Meta);

        var tools = _server.Tools.Values.Select(static registration => registration.Tool).ToArray();
        var page = Page(tools, parameters.Cursor, out var nextCursor);

        return new McpListToolsResult
        {
            Tools = page,
            NextCursor = nextCursor,
            TtlMs = 0,
            CacheScope = McpCacheScope.Private,
        };
    }

    private async ValueTask<McpResult> HandleCallToolAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpCallToolRequestParams);
        ValidateMeta(parameters.Meta);

        if (!_server.Tools.TryGetValue(parameters.Name, out var registration))
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Unknown tool '{parameters.Name}'.");
        }

        try
        {
            return await registration.Handler(
                new McpToolCallContext(this, parameters),
                cancellationToken).ConfigureAwait(false);
        }
        catch (JsonRpcException)
        {
            throw;
        }
        catch (Exception exception)
        {
            return McpToolResult.Error(exception.Message);
        }
    }

    private McpListResourcesResult HandleListResources(JsonRpcRequest request)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpListResourcesRequestParams);
        ValidateMeta(parameters.Meta);

        var resources = _server.Resources.Values.Select(static registration => registration.Resource).ToArray();
        var page = Page(resources, parameters.Cursor, out var nextCursor);

        return new McpListResourcesResult
        {
            Resources = page,
            NextCursor = nextCursor,
            TtlMs = 0,
            CacheScope = McpCacheScope.Private,
        };
    }

    private async ValueTask<McpReadResourceResult> HandleReadResourceAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpReadResourceRequestParams);
        ValidateMeta(parameters.Meta);

        if (!_server.Resources.TryGetValue(parameters.Uri, out var registration)
            && !TryFindTemplate(parameters.Uri, out registration))
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Unknown resource '{parameters.Uri}'.");
        }

        return await registration.Handler(
            new McpReadResourceContext(parameters),
            cancellationToken).ConfigureAwait(false);
    }

    private McpListResourceTemplatesResult HandleListResourceTemplates(JsonRpcRequest request)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpListResourceTemplatesRequestParams);
        ValidateMeta(parameters.Meta);

        var templates = _server.Templates.Select(static registration => registration.Template).ToArray();
        var page = Page(templates, parameters.Cursor, out var nextCursor);

        return new McpListResourceTemplatesResult
        {
            ResourceTemplates = page,
            NextCursor = nextCursor,
            TtlMs = 0,
            CacheScope = McpCacheScope.Private,
        };
    }

    private McpListPromptsResult HandleListPrompts(JsonRpcRequest request)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpListPromptsRequestParams);
        ValidateMeta(parameters.Meta);

        var prompts = _server.Prompts.Values.Select(static registration => registration.Prompt).ToArray();
        var page = Page(prompts, parameters.Cursor, out var nextCursor);

        return new McpListPromptsResult
        {
            Prompts = page,
            NextCursor = nextCursor,
            TtlMs = 0,
            CacheScope = McpCacheScope.Private,
        };
    }

    private async ValueTask<McpGetPromptResult> HandleGetPromptAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpGetPromptRequestParams);
        ValidateMeta(parameters.Meta);

        if (!_server.Prompts.TryGetValue(parameters.Name, out var registration))
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Unknown prompt '{parameters.Name}'.");
        }

        return await registration.Handler(
            new McpGetPromptContext(parameters),
            cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<McpCompleteResult> HandleCompleteAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpCompleteRequestParams);
        ValidateMeta(parameters.Meta);

        var handler = _server.CompletionHandler;
        if (handler is null)
        {
            return new McpCompleteResult
            {
                Completion = new McpCompletion { Values = Array.Empty<string>() },
            };
        }

        return await handler(parameters, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask<JsonElement> HandleListenAsync(
        JsonRpcRequest request,
        CancellationToken cancellationToken)
    {
        var parameters = Deserialize(request, McpJsonContext.Default.McpSubscriptionsListenRequestParams);
        ValidateMeta(parameters.Meta);

        if (!_server.Options.EnableSubscriptions)
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.MethodNotFound,
                "Subscriptions are disabled on this server.");
        }

        _subscriptions[request.Id] = parameters.Notifications;

        var acknowledged = new McpSubscriptionsAcknowledgedNotificationParams
        {
            Notifications = parameters.Notifications,
            Meta = CreateSubscriptionMeta(request.Id),
        };

        await SendAsync(
            McpProtocol.Notifications.SubscriptionsAcknowledged,
            JsonSerializer.SerializeToElement(
                acknowledged,
                McpJsonContext.Default.McpSubscriptionsAcknowledgedNotificationParams),
            cancellationToken).ConfigureAwait(false);

        var result = new McpSubscriptionsListenResult
        {
            Meta = new McpSubscriptionsListenResultMeta
            {
                SubscriptionId = request.Id,
                ServerInfo = CreateServerInfo(),
            },
        };

        return JsonSerializer.SerializeToElement(result, McpJsonContext.Default.McpSubscriptionsListenResult);
    }

    private bool TryFindTemplate(string uri, [NotNullWhen(true)] out McpResourceRegistration? registration)
    {
        foreach (var template in _server.Templates)
        {
            if (MatchesTemplate(template.Template.UriTemplate, uri))
            {
                registration = new McpResourceRegistration(
                    new McpResource { Uri = uri, Name = template.Template.Name },
                    template.Handler);
                return true;
            }
        }

        registration = null;
        return false;
    }

    private async ValueTask NotifySubscriptionsAsync(
        Func<McpSubscriptionFilter, bool> predicate,
        string method,
        Func<JsonRpcId, JsonElement> createParameters,
        CancellationToken cancellationToken)
    {
        foreach (var subscription in _subscriptions)
        {
            if (!predicate(subscription.Value))
            {
                continue;
            }

            await SendAsync(method, createParameters(subscription.Key), cancellationToken).ConfigureAwait(false);
        }
    }

    private ValueTask SendAsync(string method, JsonElement parameters, CancellationToken cancellationToken) =>
        _sender(new JsonRpcNotification { Method = method, Params = parameters }, cancellationToken);

    private static T Deserialize<T>(JsonRpcRequest request, JsonTypeInfo<T> typeInfo)
    {
        if (request.Params is not { } parameters)
        {
            throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Method '{request.Method}' requires parameters.");
        }

        return parameters.Deserialize(typeInfo)
            ?? throw new JsonRpcException(
                JsonRpcErrorCode.InvalidParams,
                $"Method '{request.Method}' received empty parameters.");
    }

    private static void ValidateMeta(McpRequestMeta meta)
    {
        if (string.Equals(meta.ProtocolVersion, McpProtocol.Version, StringComparison.Ordinal))
        {
            return;
        }

        var data = JsonSerializer.SerializeToElement(new
        {
            requested = meta.ProtocolVersion,
            supported = new[] { McpProtocol.Version },
        });

        throw new JsonRpcException(
            McpErrorCode.UnsupportedProtocolVersion,
            $"Unsupported protocol version '{meta.ProtocolVersion}'.",
            data);
    }

    private T[] Page<T>(IReadOnlyList<T> items, string? cursor, out string? nextCursor)
    {
        var start = 0;
        if (!string.IsNullOrEmpty(cursor)
            && (!int.TryParse(cursor, NumberStyles.None, CultureInfo.InvariantCulture, out start) || start < 0))
        {
            throw new JsonRpcException(JsonRpcErrorCode.InvalidParams, "The cursor is invalid.");
        }

        var end = Math.Min(start + _server.Options.PageSize, items.Count);
        nextCursor = end < items.Count ? end.ToString(CultureInfo.InvariantCulture) : null;

        return items.Skip(start).Take(end - start).ToArray();
    }

    private static JsonRpcResponse Success(JsonRpcId id, McpResult result) => new()
    {
        Id = id,
        Result = McpResultSerializer.Serialize(result),
    };

    private static JsonRpcResponse SuccessElement(JsonRpcId id, JsonElement result) => new()
    {
        Id = id,
        Result = result,
    };

    private static JsonRpcResponse Error(JsonRpcId id, int code, string message, JsonElement? data) => new()
    {
        Id = id,
        Error = new JsonRpcError { Code = code, Message = message, Data = data },
    };

    private McpImplementation CreateServerInfo() => new()
    {
        Name = _server.Options.Name,
        Version = _server.Options.Version,
        Title = _server.Options.Title,
    };

    private static Dictionary<string, JsonElement> CreateSubscriptionMeta(JsonRpcId id) => new()
    {
        [McpProtocol.MetaKeys.SubscriptionId] = JsonSerializer.SerializeToElement(id),
    };

    private static bool MatchesTemplate(string template, string uri)
    {
        var templateParts = template.Split('/');
        var uriParts = uri.Split('/');

        if (templateParts.Length != uriParts.Length)
        {
            return false;
        }

        for (var index = 0; index < templateParts.Length; index++)
        {
            var templatePart = templateParts[index];

            if (templatePart.Length >= 2 && templatePart[0] == '{' && templatePart[^1] == '}')
            {
                continue;
            }

            if (!string.Equals(templatePart, uriParts[index], StringComparison.Ordinal))
            {
                return false;
            }
        }

        return true;
    }
}
