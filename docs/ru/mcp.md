# Capastrum - MCP

> [English ->](../en/mcp.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.ru.md) - [Документация](../README.md)

---

## Обзор

| Пакет | Роль |
| :-- | :-- |
| `Capastrum.Mcp.Protocol` | Контракты и source-generated сериализатор MCP 2026-07-28 |
| `Capastrum.Mcp` | Сервер: инструменты, ресурсы, промпты, completion, подписки, HTTP |
| `Capastrum.Mcp.Client` | Клиент: stdio, WebSocket, HTTP, разрешение MRTR |

Ядро протокола не хранит состояние. Каждый запрос несёт версию протокола и возможности клиента в `_meta`, серверы представляются через `server/discover`, а результаты объявляют свой вид полем `resultType`. Сервер можно разместить как процесс stdio, за HTTP-эндпоинтом или поверх любого дуплексного `IJsonRpcTransport`.

---

## Сервер

```csharp
var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum-Tools",
    Version = "1.0.0",
});
```

### Инструменты

```csharp
server.RegisterTool(
    "echo",
    "Echoes a value",
    (context, _) => ValueTask.FromResult<McpResult>(
        McpToolResult.Text(context.Arguments.GetProperty("value").GetString() ?? string.Empty)));
```

`RegisterTool` принимает полный `McpTool` с явными схемами входа и выхода либо имя, описание и обработчик со сгенерированной разрешающей схемой входа. `McpToolResult` создаёт текстовые, JSON-, ошибочные результаты и результаты с требованием ввода.

### Ресурсы и шаблоны

```csharp
server.RegisterResource(
    new McpResource { Uri = "file:///readme.txt", Name = "readme" },
    (context, _) => ValueTask.FromResult(new McpReadResourceResult
    {
        Contents = new[] { new McpResourceContents { Uri = context.Uri, Text = "hello" } },
    }));

server.RegisterResourceTemplate(
    new McpResourceTemplate { UriTemplate = "file:///{path}", Name = "files" },
    (context, _) => ReadTextResourceAsync(context.Uri));
```

Точные ресурсы сопоставляются раньше шаблонов, а шаблоны проверяются в порядке регистрации.

### Промпты

```csharp
server.RegisterPrompt(
    new McpPrompt { Name = "greet" },
    (_, _) => ValueTask.FromResult(new McpGetPromptResult
    {
        Messages = new[]
        {
            new McpPromptMessage
            {
                Role = McpRole.User,
                Content = new McpTextContent { Text = "Say hello." },
            },
        },
    }));
```

Зарегистрируйте `SetCompletionHandler`, чтобы отвечать на `completion/complete` для аргументов промптов и шаблонов ресурсов.

### Контекст запроса

Обработчики инструментов получают `McpToolCallContext` с именем инструмента, `Arguments`, `Meta`, `InputResponses` и `RequestState`. `Meta` предоставляет токен прогресса, версию протокола и сведения о клиенте.

```csharp
await context.ReportProgressAsync(0.5, total: 1.0, message: "Halfway there");
await context.LogAsync(McpLoggingLevel.Info, "Indexing complete");
```

Обработчики ресурсов и промптов получают собственные типы контекста с теми же метаданными и состоянием MRTR.

### Запросы с несколькими кругами

Инструмент может запросить у клиента ввод вместо окончательного результата:

```csharp
return McpToolResult.InputRequired(
    "sampling-1",
    new McpCreateMessageInputRequest
    {
        Params = new McpCreateMessageRequestParams
        {
            MaxTokens = 100,
            Messages = new[]
            {
                new McpSamplingMessage
                {
                    Role = McpRole.User,
                    Content = JsonSerializer.SerializeToElement(prompt),
                },
            },
        },
    },
    requestState: "tool-state");
```

Результат несёт `resultType: "input_required"`, запросы ввода и непрозрачный `requestState`. Клиент разрешает ввод и повторяет вызов с `inputResponses` и тем же состоянием; обработчик читает и то и другое из своего контекста. Поддерживаются sampling, roots и elicitation.

### Подписки

```csharp
await server.NotifyToolsChangedAsync();
await server.NotifyResourcesChangedAsync();
await server.NotifyPromptsChangedAsync();
await server.NotifyResourceUpdatedAsync("file:///readme.txt");
```

Уведомления доставляются только тем потокам `subscriptions/listen`, чьи фильтры соответствуют изменению. Приняв подписку, сервер отправляет `notifications/subscriptions/acknowledged`.

### HTTP

`HandleHttpRequestAsync` принимает транспортно-нейтральный `McpHttpRequest` и возвращает `McpHttpResponse`:

```csharp
var response = await server.HandleHttpRequestAsync(
    new McpHttpRequest
    {
        Method = "POST",
        Headers = headers,
        Body = body,
    },
    cancellationToken);
```

Перед диспетчеризацией сервер проверяет `Mcp-Method` и `Mcp-Name`. Ответ подходит для эндпоинта ASP.NET Core или любого HTTP-хоста. Долгоживущим подпискам нужен потоковый адаптер сверху; `HandleHttpRequestAsync` обслуживает одиночные обмены запрос-ответ.

---

## Клиент

```csharp
await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = "my-mcp-server",
});

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync(
    "echo",
    JsonSerializer.SerializeToElement(new { value = "hello" }));
```

У `ConnectAsync` три точки входа:

| Параметры | Транспорт |
| :-- | :-- |
| `StdioMcpClientOptions` | Запускает дочерний процесс и использует stdio с разделением строк |
| `HttpMcpClientOptions` | Отправляет POST на HTTP-эндпоинт и добавляет заголовки `Mcp-Method` / `Mcp-Name` |
| `IJsonRpcTransport` + `McpClientOptions` | Любой пользовательский или уже подключённый транспорт, включая `WebSocketJsonTransport` |

При подключении клиент выполняет `server/discover` и сохраняет `ServerInfo`, `Capabilities` и `SupportedVersions`.

### Операции

| Метод | Назначение |
| :-- | :-- |
| `DiscoverAsync` | Повторно прочитать сведения о сервере |
| `ListToolsAsync` / `CallToolAsync` | Инструменты |
| `ListResourcesAsync` / `ReadResourceAsync` / `ListResourceTemplatesAsync` | Ресурсы |
| `ListPromptsAsync` / `GetPromptAsync` | Промпты |
| `CompleteAsync` | Завершение аргументов |
| `ListenAsync` | Открыть поток подписки |

### Обработчики MRTR

Задайте `SamplingHandler`, `RootsHandler` и `ElicitationHandler` в параметрах клиента. Когда инструмент возвращает `input_required`, клиент вызывает подходящий обработчик, собирает ответы под их идентификаторами и повторяет исходный вызов с `inputResponses` и `requestState`. Цикл продолжается, пока инструмент не вернёт окончательный результат или у клиента не окажется обработчика для запрошенного ввода.

### Уведомления

`NotificationReceived` предоставляет каждое необработанное уведомление. Для типовых случаев есть типизированные события:

- `ToolsListChanged`
- `ResourcesListChanged`
- `PromptsListChanged`
- `ResourceUpdated`
- `StandardErrorReceived` для дочернего процесса stdio

---

## Покрытие

Запросы:

`server/discover`, `tools/list`, `tools/call`, `resources/list`, `resources/read`, `resources/templates/list`, `prompts/list`, `prompts/get`, `completion/complete`, `subscriptions/listen`, `sampling/createMessage`, `roots/list`, `elicitation/create`.

Уведомления:

`notifications/cancelled`, `notifications/progress`, `notifications/message`, `notifications/tools/list_changed`, `notifications/resources/list_changed`, `notifications/resources/updated`, `notifications/prompts/list_changed`, `notifications/subscriptions/acknowledged`.

`arguments`, JSON-схемы, структурированное содержимое и схемы elicitation остаются `JsonElement`, потому что их форму определяет приложение.

---

## Связанные руководства

- [Ядро JSON-RPC](json-rpc.md) - конверт, peer и транспорты
- [ACP](acp.md) - сторона хоста и агента
- [Архитектура](architecture.md) - граф пакетов и проектные решения
