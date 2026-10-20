# Capastrum - ядро JSON-RPC

> [English ->](../en/json-rpc.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.ru.md) - [Документация](../README.md)

---

## Обзор

`Capastrum.JsonRpc` реализует конверт JSON-RPC 2.0, сопоставление запросов и ответов, отмену и транспорты, общие для MCP и ACP. Пакет зависит только от базовой библиотеки классов .NET 10 и использует `System.Text.Json`.

Пакет не знает ни одного из протоколов. Он передаёт сообщения JSON-RPC между двумя peer-узлами, а имена методов, формы параметров и типы результатов остаются слою выше.

---

## Модель сообщений

| Тип | Назначение |
| :-- | :-- |
| `JsonRpcRequest` | Содержит `id`, `method` и необязательные `params`; всегда ожидает ответ |
| `JsonRpcNotification` | Содержит `method` и необязательные `params`; никогда не ожидает ответ |
| `JsonRpcResponse` | Содержит `id` запроса и либо `result`, либо `error` |
| `JsonRpcError` | Содержит числовой код, сообщение и необязательные `data` |
| `JsonRpcId` | Числовой или строковый идентификатор запроса; свойство `Text` возвращает его проводную форму |

`params` и `result` в ядре остаются `JsonElement`. Протокольные пакеты десериализуют их в собственные source-generated контракты, поэтому ядро не создаёт промежуточный граф объектов.

---

## Peer

`JsonRpcPeer` владеет одним циклом чтения, сопоставляет ответы с исходящими запросами и диспетчеризует входящие запросы и уведомления.

```csharp
await using var peer = new JsonRpcPeer(transport);

peer.RequestHandler = async (request, cancellationToken) =>
{
    var result = await HandleAsync(request.Method, request.Params, cancellationToken);
    return new JsonRpcResponse { Id = request.Id, Result = result };
};

peer.NotificationReceived += notification => Console.WriteLine(notification.Method);

var response = await peer.InvokeAsync("tools/list");
await peer.NotifyAsync("notifications/cancelled");
```

### Поведение

- `InvokeAsync` выделяет идентификатор запроса, отправляет запрос и ждёт соответствующий ответ.
- `RequestHandler` отвечает на входящие запросы. Исключение превращается в ответ с ошибкой JSON-RPC и текстом исключения.
- `NotificationReceived` вызывается для каждого входящего уведомления.
- Отменённый `InvokeAsync` перестаёт ждать и удаляет запись из ожидающих. Решение об отправке уведомления об отмене принимает протокольный слой.
- `DisposeAsync` завершает все ожидающие запросы ошибкой и освобождает транспорт.

### Типизированные ответы

Протокольные пакеты передают source-generated метаданные явно:

```csharp
var result = await peer.InvokeAsync(
    "tools/list",
    parameters: null,
    McpJsonContext.Default.McpListToolsResult);
```

Перегрузка возвращает типизированный результат без рефлексии.

---

## Транспорты

| Транспорт | Применение |
| :-- | :-- |
| `NewlineJsonTransport` | stdio и другие дуплексные потоки; одно сообщение JSON на строку |
| `WebSocketJsonTransport` | текстовые кадры WebSocket |
| `HttpJsonTransport` | HTTP POST с ответом в JSON |

Все транспорты реализуют `IJsonRpcTransport` с методами `SendAsync` и `ReadAsync`. Транспорт отвечает за кадрирование, а не за семантику протокола.

`HttpJsonTransport` принимает `HttpClient`, которым владеет вызывающий код, и предоставляет `ConfigureRequest`, позволяя протокольному слою добавлять заголовки к каждому исходящему запросу. Уведомление завершается, как только эндпоинт принял POST; запрос читает тело ответа JSON.

`NewlineJsonTransport` использует `System.IO.Pipelines` для кадрирования и обрабатывает последнее сообщение, даже если поток заканчивается без завершающего перевода строки.

---

## Ошибки

`JsonRpcPeer` выбрасывает `JsonRpcException` при ответах с ошибкой. Исключение предоставляет `Code`, `Message` и `Data`.

Стандартные коды доступны в `JsonRpcErrorCode`:

| Константа | Код | Значение |
| :-- | --: | :-- |
| `ParseError` | -32700 | Некорректный JSON |
| `InvalidRequest` | -32600 | Корректный JSON, некорректный запрос |
| `MethodNotFound` | -32601 | Метод неизвестен или недоступен |
| `InvalidParams` | -32602 | Некорректные параметры метода |
| `InternalError` | -32603 | Внутренняя ошибка |

Коды конкретных протоколов находятся в пакетах MCP и ACP.

---

## Отмена

Отмена кооперативная:

- Отменённый `InvokeAsync` удаляет ожидаемый ответ и перестаёт ждать.
- Peer не отправляет `$/cancel_request` или `notifications/cancelled` самостоятельно; протокольный слой знает, какое уведомление ожидает другая сторона.
- Освобождение peer завершает каждый ожидающий вызов `ObjectDisposedException`, а не оставляет его висеть.

---

## Производительность

- Source-generated сериализация конверта и каждого протокольного payload.
- `System.IO.Pipelines` для кадрирования строк.
- Один цикл чтения на peer и одна блокировка записи на транспорт.
- Никакого рефлексивного запасного сериализатора на протокольных путях.
- Бюджеты аллокаций отслеживаются в `benchmarks/Capastrum.Benchmarks`: замеряются сериализация конверта и полный круговой запрос в памяти.

---

## Связанные руководства

- [MCP](mcp.md) - инструменты, ресурсы, промпты, MRTR и подписки
- [ACP](acp.md) - хост, агент, разрешения, файлы и терминалы
- [Архитектура](architecture.md) - граф пакетов и проектные ограничения
