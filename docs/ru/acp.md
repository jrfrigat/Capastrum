# Capastrum - ACP

> [English ->](../en/acp.md) - [README](https://github.com/jrfrigat/Capastrum/blob/main/README.ru.md) - [Документация](../README.md)

---

## Обзор

| Пакет | Роль |
| :-- | :-- |
| `Capastrum.Acp.Protocol` | Контракты и source-generated сериализатор ACP v1 |
| `Capastrum.Acp` | Хост: запускает агента, обслуживает его запросы, применяет разрешения |
| `Capastrum.Acp.Server` | Агент: сессии, промпты, обновления, исходящие запросы |

ACP соединяет редактор или другой хост с агентом через stdio. Хост владеет процессом, рабочим пространством и границей безопасности; агент запрашивает разрешение перед запуском команды или записью файла. Capastrum реализует обе стороны на одних и тех же типизированных контрактах.

---

## Хост

```csharp
await using var agent = await AcpAgentClient.StartAsync(new AcpClientOptions
{
    Executable = "npx",
    Arguments = ["-y", "@zed-industries/claude-code-acp"],
    WorkspaceRoot = "/path/to/project",
});
```

`StartAsync` запускает процесс, подключается через stdio с разделением строк и выполняет `initialize`. Используйте `ConnectAsync` с готовым `IJsonRpcTransport` для тестов, пользовательских транспортов или уже запущенного агента.

### Жизненный цикл сессии

```csharp
var session = await agent.NewSessionAsync("/path/to/project");

var response = await agent.SendPromptAsync(
    session.SessionId,
    [new AcpTextContent { Text = "Find outdated dependencies." }]);

await agent.CloseSessionAsync(session.SessionId);
```

Хост также предоставляет `LoadSessionAsync`, `ResumeSessionAsync`, `ListSessionsAsync`, `DeleteSessionAsync`, `SetModeAsync` и `SetConfigOptionAsync`. `AuthenticateAsync` и `LogoutAsync` покрывают методы аутентификации, которые объявляет агент.

### Human-in-the-loop

`OnActionConfirm` вызывается для каждого `session/request_permission`:

```csharp
agent.OnActionConfirm((request, _) => ValueTask.FromResult(
    request.Command?.Contains("rm -rf", StringComparison.Ordinal) == true
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed));
```

`AcpPermissionRequest` предоставляет сессию, вызов инструмента, его заголовок, имя, вид, сырой ввод и команду терминала, когда её удаётся извлечь. Верните один из результатов:

| Результат | Итог |
| :-- | :-- |
| `ConfirmationResult.Allowed` | Выбирает первую опцию `allow_once`; при её отсутствии возвращает `cancelled` |
| `ConfirmationResult.Denied` | Выбирает первую опцию `reject_once`; при её отсутствии возвращает `cancelled` |
| `ConfirmationResult.Selected(optionId)` | Выбирает указанную опцию |

Если обработчик не зарегистрирован, каждый запрос разрешения отклоняется. Разрешение никогда не выдаётся по умолчанию.

### Файловая система

```csharp
agent.OnReadTextFile((request, _) => ValueTask.FromResult(
    new AcpReadTextFileResponse { Content = File.ReadAllText(request.Path) }));

agent.OnWriteTextFile((request, _) =>
{
    File.WriteAllText(request.Path, request.Content);
    return ValueTask.FromResult(new AcpWriteTextFileResponse());
});
```

Без соответствующего обработчика агент получает ошибку JSON-RPC «метод не найден». Хост решает, какие пути допустимы; Capastrum не добавляет неявную песочницу.

### Терминалы

`AcpTerminalHost` запускает процессы, запрошенные агентом, буферизует их вывод до указанного предела и реализует `terminal/output`, `terminal/wait_for_exit`, `terminal/kill` и `terminal/release`. `AcpAgentClient` создаёт его по умолчанию и владеет его временем жизни. Собственный хост можно передать через `AcpClientOptions.TerminalHost`.

### Обновления и отмена

```csharp
agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
```

Уведомления `session/update` вызывают `SessionUpdated`; `update.Text` возвращает текст обновлений с текстовыми фрагментами. `CancelAsync(sessionId)` отправляет `session/cancel`. Отмена токена, переданного в `SendPromptAsync`, делает то же самое, а агент должен ответить `stopReason=cancelled`.

---

## Агент

```csharp
var server = new AcpAgentServer(new AcpAgentServerOptions
{
    AgentInfo = new AcpImplementation { Name = "MyAgent", Version = "1.0.0" },
    NewSessionHandler = (_, _) => ValueTask.FromResult(new AcpNewSessionResponse
    {
        SessionId = Guid.NewGuid().ToString("N"),
    }),
    PromptHandler = async (context, cancellationToken) =>
    {
        await context.SendUpdateAsync(
            new AcpAgentMessageChunk
            {
                Content = new AcpTextContent { Text = "Hello from C#." },
            },
            cancellationToken);

        return new AcpPromptResponse { StopReason = AcpStopReason.EndTurn };
    },
});

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
```

`AgentInfo` и `PromptHandler` обязательны. Все остальные обработчики необязательны; запрос к ненастроенному методу возвращает ошибку JSON-RPC «метод не найден».

### Контекст промпта

`AcpPromptContext` - это представление хоста со стороны агента:

| Элемент | Назначение |
| :-- | :-- |
| `SendUpdateAsync` | Отправить уведомление `session/update` |
| `RequestPermissionAsync` | Попросить хост разрешить вызов инструмента |
| `ReadTextFileAsync` / `WriteTextFileAsync` | Доступ к рабочему пространству |
| `CreateTerminalAsync` / `GetTerminalOutputAsync` / `WaitForTerminalExitAsync` / `KillTerminalAsync` / `ReleaseTerminalAsync` | Запуск и управление терминалами |
| `ElicitAsync` | Запросить у пользователя структурированный ввод |

Ответ на запрос разрешения содержит выбранную хостом опцию или результат отмены. Агент обязан соблюдать его до выполнения вызова инструмента.

### Обработчики сессий

`AcpAgentServerOptions` предоставляет обработчики для `Authenticate`, `Logout`, `NewSession`, `LoadSession`, `ResumeSession`, `ListSessions`, `CloseSession`, `DeleteSession`, `SetMode`, `SetConfigOption` и `Prompt`. Состояние принадлежит вашему приложению; сервер маршрутизирует запросы и владеет протокольным соединением.

### Отмена

Сервер наблюдает `session/cancel`. Токен, переданный в `PromptHandler`, отменяется, когда хост отменяет ход. Обработчик, который не может остановиться немедленно, всё равно должен вернуть `AcpPromptResponse` с `StopReason = AcpStopReason.Cancelled`.

---

## Покрытие

Запросы:

`initialize`, `authenticate`, `logout`, `session/new`, `session/load`, `session/resume`, `session/list`, `session/close`, `session/delete`, `session/set_mode`, `session/set_config_option`, `session/prompt`, `session/request_permission`, `fs/read_text_file`, `fs/write_text_file`, `terminal/create`, `terminal/output`, `terminal/wait_for_exit`, `terminal/kill`, `terminal/release`, `elicitation/create`.

Уведомления:

`session/update`, `session/cancel`, `elicitation/complete`.

`rawInput`, `rawOutput`, схемы elicitation, варианты выбора и payload расширений остаются `JsonElement`.

---

## Безопасность

- Нет обработчика разрешений - значит, запрещено. Зарегистрируйте его до запуска сессии в недоверенном рабочем пространстве.
- Обработчики файлов явные. Отсутствующий обработчик - это ошибка, а не неограниченный доступ.
- Процессы терминалов работают с идентичностью хоста. Ограничьте рабочее пространство и проверяйте запросы разрешений перед одобрением.
- `ConfirmationResult.Allowed` соответствует `allow_once`; он не выдаёт разрешение на всю сессию или навсегда.

---

## Связанные руководства

- [Ядро JSON-RPC](json-rpc.md) - конверт, peer и транспорты
- [MCP](mcp.md) - инструменты, ресурсы и промпты
- [Архитектура](architecture.md) - граф пакетов и проектные решения
