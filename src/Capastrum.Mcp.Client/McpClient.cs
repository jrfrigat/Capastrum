using System.Diagnostics;
using System.Text.Json;
using Capastrum.JsonRpc;
using Capastrum.JsonRpc.Transports;
using Capastrum.Mcp.Protocol;
using Capastrum.Mcp.Protocol.Serialization;

namespace Capastrum.Mcp.Client;

/// <summary>
/// A Model Context Protocol client.
/// </summary>
public sealed class McpClient : IAsyncDisposable, IDisposable
{
    private readonly JsonRpcPeer _peer;
    private readonly McpClientOptions _options;
    private Process? _process;
    private bool _disposed;

    private McpClient(JsonRpcPeer peer, McpClientOptions options)
    {
        _peer = peer;
        _options = options;
        _peer.NotificationReceived += OnNotification;
    }

    /// <summary>The server implementation reported by <c>server/discover</c>.</summary>
    public McpImplementation? ServerInfo { get; private set; }

    /// <summary>The server capabilities.</summary>
    public McpServerCapabilities? Capabilities { get; private set; }

    /// <summary>The protocol versions the server supports.</summary>
    public IReadOnlyList<string> SupportedVersions { get; private set; } = [];

    /// <summary>Natural-language guidance from the server.</summary>
    public string? Instructions { get; private set; }

    /// <summary>Raised for every notification.</summary>
    public event Action<JsonRpcNotification>? NotificationReceived;

    /// <summary>Raised when the tool list changes.</summary>
    public event Action? ToolsListChanged;

    /// <summary>Raised when the resource list changes.</summary>
    public event Action? ResourcesListChanged;

    /// <summary>Raised when a resource changes.</summary>
    public event Action<string>? ResourceUpdated;

    /// <summary>Raised when the prompt list changes.</summary>
    public event Action? PromptsListChanged;

    /// <summary>Raised for every line the child process writes to stderr.</summary>
    public event Action<string>? StandardErrorReceived;

    /// <summary>
    /// Connects over an existing transport and performs <c>server/discover</c>.
    /// </summary>
    public static async Task<McpClient> ConnectAsync(
        IJsonRpcTransport transport,
        McpClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentNullException.ThrowIfNull(options);

        var peer = new JsonRpcPeer(transport);
        var client = new McpClient(peer, options);

        try
        {
            await client.DiscoverAsync(cancellationToken).ConfigureAwait(false);
            return client;
        }
        catch
        {
            await client.DisposeAsync().ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Starts a child process and connects to it over stdio.
    /// </summary>
    public static async Task<McpClient> ConnectAsync(
        StdioMcpClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var startInfo = new ProcessStartInfo(options.Command)
        {
            UseShellExecute = false,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        foreach (var argument in options.Arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        if (!string.IsNullOrEmpty(options.WorkingDirectory))
        {
            startInfo.WorkingDirectory = options.WorkingDirectory;
        }

        if (options.Environment is not null)
        {
            foreach (var variable in options.Environment)
            {
                startInfo.Environment[variable.Key] = variable.Value;
            }
        }

        var process = Process.Start(startInfo)
            ?? throw new InvalidOperationException($"Could not start '{options.Command}'.");

        var transport = new NewlineJsonTransport(
            process.StandardInput.BaseStream,
            process.StandardOutput.BaseStream);

        try
        {
            var client = await ConnectAsync(transport, options, cancellationToken).ConfigureAwait(false);
            client.AttachProcess(process);
            return client;
        }
        catch
        {
            process.Kill(entireProcessTree: true);
            process.Dispose();
            throw;
        }
    }

    /// <summary>
    /// Connects to an HTTP endpoint.
    /// </summary>
    public static Task<McpClient> ConnectAsync(
        HttpMcpClientOptions options,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);

        var ownsClient = options.HttpClient is null;
        var httpClient = options.HttpClient ?? new HttpClient();
        var transport = new HttpJsonTransport(httpClient, options.Endpoint, ownsClient)
        {
            ConfigureRequest = (request, message) =>
            {
                var method = message switch
                {
                    JsonRpcRequest rpcRequest => rpcRequest.Method,
                    JsonRpcNotification notification => notification.Method,
                    _ => null,
                };

                if (method is not null)
                {
                    request.Headers.TryAddWithoutValidation(McpProtocol.Headers.Method, method);
                }

                if (message is JsonRpcRequest rpc
                    && rpc.Method is McpProtocol.Methods.CallTool or McpProtocol.Methods.GetPrompt
                    && rpc.Params is { ValueKind: JsonValueKind.Object } parameters
                    && parameters.TryGetProperty("name", out var name)
                    && name.ValueKind == JsonValueKind.String)
                {
                    request.Headers.TryAddWithoutValidation(McpProtocol.Headers.Name, name.GetString());
                }

                if (options.Headers is not null)
                {
                    foreach (var header in options.Headers)
                    {
                        request.Headers.TryAddWithoutValidation(header.Key, header.Value);
                    }
                }
            },
        };

        return ConnectAsync(transport, options, cancellationToken);
    }

    /// <summary>
    /// Performs <c>server/discover</c> and records the server capabilities.
    /// </summary>
    public async ValueTask<McpDiscoverResult> DiscoverAsync(CancellationToken cancellationToken = default)
    {
        var parameters = new McpDiscoverRequestParams { Meta = CreateMeta() };
        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.Discover,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpDiscoverRequestParams),
            cancellationToken).ConfigureAwait(false);

        var discover = result.Deserialize(McpJsonContext.Default.McpDiscoverResult)
            ?? throw new JsonRpcException(JsonRpcErrorCode.InternalError, "The server returned an empty discover result.");

        if (!discover.SupportedVersions.Contains(McpProtocol.Version, StringComparer.Ordinal))
        {
            throw new JsonRpcException(
                McpErrorCode.UnsupportedProtocolVersion,
                $"The server does not support MCP {McpProtocol.Version}.");
        }

        ServerInfo = discover.Meta?.ServerInfo;
        Capabilities = discover.Capabilities;
        SupportedVersions = discover.SupportedVersions;
        Instructions = discover.Instructions;

        return discover;
    }

    /// <summary>Lists tools.</summary>
    public async ValueTask<McpListToolsResult> ListToolsAsync(
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new McpListToolsRequestParams { Meta = CreateMeta(), Cursor = cursor };
        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.ListTools,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpListToolsRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpListToolsResult)
            ?? throw EmptyResult(McpProtocol.Methods.ListTools);
    }

    /// <summary>Calls a tool, resolving input requests as needed.</summary>
    public async ValueTask<McpCallToolResult> CallToolAsync(
        string name,
        JsonElement? arguments = null,
        JsonRpcId? progressToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameters = new McpCallToolRequestParams
        {
            Meta = CreateMeta(progressToken),
            Name = name,
            Arguments = arguments,
        };

        var result = await InvokeWithInputAsync(
            McpProtocol.Methods.CallTool,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpCallToolRequestParams),
            (input, responses) => JsonSerializer.SerializeToElement(
                parameters with { InputResponses = responses, RequestState = input.RequestState },
                McpJsonContext.Default.McpCallToolRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpCallToolResult)
            ?? throw EmptyResult(McpProtocol.Methods.CallTool);
    }

    /// <summary>Lists resources.</summary>
    public async ValueTask<McpListResourcesResult> ListResourcesAsync(
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new McpListResourcesRequestParams { Meta = CreateMeta(), Cursor = cursor };
        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.ListResources,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpListResourcesRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpListResourcesResult)
            ?? throw EmptyResult(McpProtocol.Methods.ListResources);
    }

    /// <summary>Reads a resource, resolving input requests as needed.</summary>
    public async ValueTask<McpReadResourceResult> ReadResourceAsync(
        string uri,
        JsonRpcId? progressToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(uri);

        var parameters = new McpReadResourceRequestParams
        {
            Meta = CreateMeta(progressToken),
            Uri = uri,
        };

        var result = await InvokeWithInputAsync(
            McpProtocol.Methods.ReadResource,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpReadResourceRequestParams),
            (input, responses) => JsonSerializer.SerializeToElement(
                parameters with { InputResponses = responses, RequestState = input.RequestState },
                McpJsonContext.Default.McpReadResourceRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpReadResourceResult)
            ?? throw EmptyResult(McpProtocol.Methods.ReadResource);
    }

    /// <summary>Lists resource templates.</summary>
    public async ValueTask<McpListResourceTemplatesResult> ListResourceTemplatesAsync(
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new McpListResourceTemplatesRequestParams { Meta = CreateMeta(), Cursor = cursor };
        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.ListResourceTemplates,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpListResourceTemplatesRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpListResourceTemplatesResult)
            ?? throw EmptyResult(McpProtocol.Methods.ListResourceTemplates);
    }

    /// <summary>Lists prompts.</summary>
    public async ValueTask<McpListPromptsResult> ListPromptsAsync(
        string? cursor = null,
        CancellationToken cancellationToken = default)
    {
        var parameters = new McpListPromptsRequestParams { Meta = CreateMeta(), Cursor = cursor };
        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.ListPrompts,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpListPromptsRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpListPromptsResult)
            ?? throw EmptyResult(McpProtocol.Methods.ListPrompts);
    }

    /// <summary>Gets a prompt, resolving input requests as needed.</summary>
    public async ValueTask<McpGetPromptResult> GetPromptAsync(
        string name,
        IReadOnlyDictionary<string, string>? arguments = null,
        JsonRpcId? progressToken = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var parameters = new McpGetPromptRequestParams
        {
            Meta = CreateMeta(progressToken),
            Name = name,
            Arguments = arguments,
        };

        var result = await InvokeWithInputAsync(
            McpProtocol.Methods.GetPrompt,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpGetPromptRequestParams),
            (input, responses) => JsonSerializer.SerializeToElement(
                parameters with { InputResponses = responses, RequestState = input.RequestState },
                McpJsonContext.Default.McpGetPromptRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpGetPromptResult)
            ?? throw EmptyResult(McpProtocol.Methods.GetPrompt);
    }

    /// <summary>Requests argument completion.</summary>
    public async ValueTask<McpCompleteResult> CompleteAsync(
        McpCompletionReference reference,
        McpCompletionArgument argument,
        McpCompletionContext? context = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(reference);
        ArgumentNullException.ThrowIfNull(argument);

        var parameters = new McpCompleteRequestParams
        {
            Meta = CreateMeta(),
            Ref = reference,
            Argument = argument,
            Context = context,
        };

        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.Complete,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpCompleteRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpCompleteResult)
            ?? throw EmptyResult(McpProtocol.Methods.Complete);
    }

    /// <summary>Opens a subscription stream.</summary>
    public async ValueTask<McpSubscriptionsListenResult> ListenAsync(
        McpSubscriptionFilter filter,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filter);

        var parameters = new McpSubscriptionsListenRequestParams
        {
            Meta = CreateMeta(),
            Notifications = filter,
        };

        var result = await _peer.InvokeAsync(
            McpProtocol.Methods.Listen,
            JsonSerializer.SerializeToElement(parameters, McpJsonContext.Default.McpSubscriptionsListenRequestParams),
            cancellationToken).ConfigureAwait(false);

        return result.Deserialize(McpJsonContext.Default.McpSubscriptionsListenResult)
            ?? throw EmptyResult(McpProtocol.Methods.Listen);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _peer.NotificationReceived -= OnNotification;
        await _peer.DisposeAsync().ConfigureAwait(false);

        var process = _process;
        _process = null;

        if (process is not null)
        {
            try
            {
                if (!process.HasExited)
                {
                    process.Kill(entireProcessTree: true);
                }
            }
            catch (InvalidOperationException)
            {
            }
            catch (System.ComponentModel.Win32Exception)
            {
            }

            process.Dispose();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        DisposeAsync().AsTask().GetAwaiter().GetResult();
        GC.SuppressFinalize(this);
    }

    private async ValueTask<JsonElement> InvokeWithInputAsync(
        string method,
        JsonElement initialParameters,
        Func<McpInputRequiredResult, Dictionary<string, JsonElement>, JsonElement> retry,
        CancellationToken cancellationToken)
    {
        var parameters = initialParameters;

        for (var round = 0; round < 8; round++)
        {
            var result = await _peer.InvokeAsync(method, parameters, cancellationToken).ConfigureAwait(false);

            if (!IsInputRequired(result))
            {
                return result;
            }

            var input = result.Deserialize(McpJsonContext.Default.McpInputRequiredResult)
                ?? throw new JsonRpcException(
                    JsonRpcErrorCode.InternalError,
                    "The server returned an empty input-required result.");

            var responses = await ResolveInputsAsync(input, cancellationToken).ConfigureAwait(false);
            parameters = retry(input, responses);
        }

        throw new JsonRpcException(
            JsonRpcErrorCode.InternalError,
            "The server requested too many input rounds.");
    }

    private async ValueTask<Dictionary<string, JsonElement>> ResolveInputsAsync(
        McpInputRequiredResult input,
        CancellationToken cancellationToken)
    {
        var responses = new Dictionary<string, JsonElement>();

        foreach (var (id, request) in input.InputRequests ?? new Dictionary<string, McpInputRequest>())
        {
            switch (request)
            {
                case McpCreateMessageInputRequest sampling:
                    var samplingHandler = _options.SamplingHandler
                        ?? throw new InvalidOperationException("The server requested sampling, but no sampling handler is configured.");
                    var samplingResult = await samplingHandler(sampling.Params, cancellationToken).ConfigureAwait(false);
                    responses[id] = JsonSerializer.SerializeToElement(
                        samplingResult,
                        McpJsonContext.Default.McpCreateMessageResult);
                    break;

                case McpListRootsInputRequest:
                    var rootsHandler = _options.RootsHandler
                        ?? throw new InvalidOperationException("The server requested roots, but no roots handler is configured.");
                    var rootsResult = await rootsHandler(cancellationToken).ConfigureAwait(false);
                    responses[id] = JsonSerializer.SerializeToElement(
                        rootsResult,
                        McpJsonContext.Default.McpListRootsResult);
                    break;

                case McpElicitInputRequest elicitation:
                    var elicitationHandler = _options.ElicitationHandler
                        ?? throw new InvalidOperationException("The server requested elicitation, but no elicitation handler is configured.");
                    var elicitationResult = await elicitationHandler(elicitation.Params, cancellationToken).ConfigureAwait(false);
                    responses[id] = JsonSerializer.SerializeToElement(
                        elicitationResult,
                        McpJsonContext.Default.McpElicitResult);
                    break;

                default:
                    throw new InvalidOperationException($"Unsupported input request type '{request.GetType()}'.");
            }
        }

        return responses;
    }

    private void OnNotification(JsonRpcNotification notification)
    {
        NotificationReceived?.Invoke(notification);

        switch (notification.Method)
        {
            case McpProtocol.Notifications.ToolsListChanged:
                ToolsListChanged?.Invoke();
                break;

            case McpProtocol.Notifications.ResourcesListChanged:
                ResourcesListChanged?.Invoke();
                break;

            case McpProtocol.Notifications.PromptsListChanged:
                PromptsListChanged?.Invoke();
                break;

            case McpProtocol.Notifications.ResourceUpdated when notification.Params is { } parameters:
                var updated = parameters.Deserialize(McpJsonContext.Default.McpResourceUpdatedNotificationParams);
                if (updated is not null)
                {
                    ResourceUpdated?.Invoke(updated.Uri);
                }

                break;
        }
    }

    private McpRequestMeta CreateMeta(JsonRpcId? progressToken = null) => new()
    {
        ProtocolVersion = McpProtocol.Version,
        ClientCapabilities = _options.ClientCapabilities,
        ClientInfo = _options.ClientInfo,
        ProgressToken = progressToken,
    };

    private static bool IsInputRequired(JsonElement result) =>
        result.ValueKind == JsonValueKind.Object
        && result.TryGetProperty("resultType", out var resultType)
        && resultType.ValueKind == JsonValueKind.String
        && string.Equals(resultType.GetString(), McpProtocol.ResultTypes.InputRequired, StringComparison.Ordinal);

    private static JsonRpcException EmptyResult(string method) => new(
        JsonRpcErrorCode.InternalError,
        $"The server returned an empty result for '{method}'.");

    private void AttachProcess(Process process)
    {
        _process = process;

        _ = Task.Run(async () =>
        {
            try
            {
                while (await process.StandardError.ReadLineAsync().ConfigureAwait(false) is { } line)
                {
                    StandardErrorReceived?.Invoke(line);
                }
            }
            catch (IOException)
            {
            }
            catch (ObjectDisposedException)
            {
            }
        });
    }
}
