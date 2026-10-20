<p align="center">
  <a href="docs/README.md">
    <img src="assets/banner.svg" alt="Capastrum - MCP и ACP для .NET 10" width="860">
  </a>
</p>

# Capastrum - MCP + ACP для .NET

<p align="center">🌐 <a href="README.md">English</a> | <b>Русский</b></p>

[![CI](https://github.com/jrfrigat/Capastrum/actions/workflows/ci.yml/badge.svg)](https://github.com/jrfrigat/Capastrum/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/badge/NuGet-7%20packages-004880?logo=nuget)](https://www.nuget.org/packages?q=Capastrum)
[![.NET](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/)
[![MCP](https://img.shields.io/badge/MCP-2026--07--28-14B8A6)](https://modelcontextprotocol.io/)
[![ACP](https://img.shields.io/badge/ACP-v1-8B5CF6)](https://agentclientprotocol.com/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

Capastrum - **"швейцарский нож" для C#-разработчиков, которые работают с ИИ-моделями и агентами**. Семь независимых NuGet-пакетов реализуют **Model Context Protocol 2026-07-28** и **Agent Client Protocol v1** как строго типизированные контракты JSON-RPC 2.0. Соберите MCP-сервер, подключитесь к чужому серверу, запускайте и контролируйте ACP-агента или предоставьте своё приложение как агента - без фреймворка, среды выполнения агентов и движка логирования.

**Инструмент, а не фреймворк.** Каждый пакет делает одну задачу и не вмешивается в архитектуру приложения. Ваши DI, конфигурация, логирование и жизненный цикл остаются вашими; Capastrum даёт только протокольный слой.

**Полное покрытие в обе стороны.** MCP-сервер и клиент, ACP-хост и агент с типизированными запросами, результатами и уведомлениями, подписками, отменой и сценариями с подтверждением действий, без которых протоколы не работают в продакшене.

**Быстрота по построению.** Source generation в `System.Text.Json`, кадрирование через `System.IO.Pipelines` и продуманные горячие пути не дают протокольному слою мешать приложению.

**7 пакетов - MCP-сервер + клиент - ACP-хост + агент - stdio / WebSocket / HTTP - .NET 10 - MIT**

---

## Возможности

- **Полное покрытие MCP 2026-07-28** - `server/discover`, инструменты, ресурсы, шаблоны ресурсов, промпты, completion, подписки, sampling, roots и elicitation
- **Полное покрытие ACP v1** - инициализация и аутентификация, сессии, промпты, обновления сессий, разрешения, файловая система, терминалы, elicitation и отмена
- **Обе стороны каждого протокола** - отдавайте инструменты через `Capastrum.Mcp`, подключайтесь к внешним серверам через `Capastrum.Mcp.Client`, оркестрируйте агентов через `Capastrum.Acp`, предоставляйте собственного агента через `Capastrum.Acp.Server`
- **Human-in-the-loop по умолчанию** - ACP-хост без обработчика разрешений запрещает каждое действие инструмента, а не разрешает его молча
- **Типизированные контракты от начала до конца** - сгенерированные сериализаторы, записи и перечисления для каждой формы протокола; пользовательские данные остаются `JsonElement`
- **Три транспорта** - JSON с разделением строк в stdio, текстовые кадры WebSocket и HTTP POST с заголовками протокола MCP
- **MRTR из коробки** - инструмент может вернуть `input_required`; клиент разрешает sampling, roots или elicitation и повторяет вызов с сохранённым состоянием обработчика
- **Подписки и уведомления** - отфильтрованные потоки `subscriptions/listen` об изменениях инструментов, ресурсов и промптов
- **Независимые пакеты** - подключайте только нужный слой; у `Capastrum.JsonRpc` нет зависимостей вне базовой библиотеки классов
- **Проверено сквозными тестами** - conformance-тесты покрывают поверхность каждого протокола и полную цепочку ACP-хост -> агент -> MCP-клиент -> MCP-сервер
- **Замеренные горячие пути** - наборы BenchmarkDotNet для конвертов, круговых запросов peer и диспетчеризации инструментов MCP

---

## Быстрый старт

Выберите пакеты для нужной стороны протокола:

| Что вы создаёте | Что установить |
| --- | --- |
| MCP-сервер | `Capastrum.Mcp` |
| MCP-клиент | `Capastrum.Mcp.Client` |
| ACP-хост, управляющий агентом | `Capastrum.Acp` |
| ACP-агент | `Capastrum.Acp.Server` |

```sh
dotnet add package Capastrum.Mcp
dotnet add package Capastrum.Mcp.Client
dotnet add package Capastrum.Acp
dotnet add package Capastrum.Acp.Server
```

### MCP-сервер

```csharp
using Capastrum.Mcp;
using Capastrum.Mcp.Protocol;

var server = new McpServer(new McpServerOptions
{
    Name = "Capastrum-Tools",
    Version = "1.0.0",
});

server.RegisterTool(
    "echo",
    "Echoes a value",
    (context, _) => ValueTask.FromResult<McpResult>(
        McpToolResult.Text(context.Arguments.GetProperty("value").GetString() ?? string.Empty)));

await server.RunAsync(Console.OpenStandardInput(), Console.OpenStandardOutput());
```

`McpServer` также регистрирует ресурсы, шаблоны ресурсов и промпты, обслуживает `server/discover`, принимает HTTP через `HandleHttpRequestAsync` и отправляет отфильтрованные уведомления через `NotifyToolsChangedAsync`, `NotifyResourcesChangedAsync` и `NotifyPromptsChangedAsync`.

### MCP-клиент

```csharp
using System.Text.Json;
using Capastrum.Mcp.Client;

await using var client = await McpClient.ConnectAsync(new StdioMcpClientOptions
{
    Command = "my-mcp-server",
});

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync(
    "echo",
    JsonSerializer.SerializeToElement(new { value = "hello" }));
```

При подключении клиент выполняет `server/discover` и разрешает многораундовые запросы через обработчики `SamplingHandler`, `RootsHandler` и `ElicitationHandler`. Для HTTP-эндпоинта используйте `HttpMcpClientOptions`.

### ACP-хост

```csharp
using Capastrum.Acp;
using Capastrum.Acp.Protocol;

await using var agent = await AcpAgentClient.StartAsync(new AcpClientOptions
{
    Executable = "npx",
    Arguments = ["-y", "@zed-industries/claude-code-acp"],
    WorkspaceRoot = "/path/to/project",
});

agent.OnSessionUpdate(update => Console.WriteLine(update.Text));
agent.OnActionConfirm((request, _) => ValueTask.FromResult(
    request.Command?.Contains("rm -rf", StringComparison.Ordinal) == true
        ? ConfirmationResult.Denied
        : ConfirmationResult.Allowed));

var session = await agent.NewSessionAsync("/path/to/project");
await agent.SendPromptAsync(
    session.SessionId,
    [new AcpTextContent { Text = "Find outdated dependencies and update them." }]);
```

`OnActionConfirm` - это граница безопасности: проверяйте каждую команду терминала и правку файла до их выполнения. Доступ к файлам и elicitation регистрируются так же через `OnReadTextFile`, `OnWriteTextFile` и `OnElicitation`.

### ACP-агент

```csharp
using Capastrum.Acp.Protocol;
using Capastrum.Acp.Server;

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

Внутри хода промпта `AcpPromptContext` предоставляет `SendUpdateAsync`, `RequestPermissionAsync`, структурированный доступ к терминалам и файловой системе, а также `ElicitAsync`.

---

## Пакеты

| Пакет | Назначение | Зависит от |
| --- | --- | --- |
| `Capastrum.JsonRpc` | Peer JSON-RPC 2.0, модель сообщений, транспорты | - |
| `Capastrum.Mcp.Protocol` | Контракты и сериализатор MCP 2026-07-28 | `Capastrum.JsonRpc` |
| `Capastrum.Mcp` | MCP-сервер: инструменты, ресурсы, промпты, HTTP | `Capastrum.Mcp.Protocol` |
| `Capastrum.Mcp.Client` | MCP-клиент: stdio, WebSocket и HTTP | `Capastrum.Mcp.Protocol` |
| `Capastrum.Acp.Protocol` | Контракты и сериализатор ACP v1 | `Capastrum.JsonRpc` |
| `Capastrum.Acp` | ACP-хост: запуск процесса, разрешения, файлы, терминалы | `Capastrum.Acp.Protocol` |
| `Capastrum.Acp.Server` | Сервер ACP-агента | `Capastrum.Acp.Protocol` |

Каждый пакет публикуется отдельно. Общего пакета-зонтика нет, поэтому приложение никогда не несёт реализацию протокола, которую не использует.

---

## Архитектура

Зависимости направлены в одну сторону, а два протокола никогда не ссылаются друг на друга:

```text
Capastrum.JsonRpc
|- Capastrum.Mcp.Protocol
|  |- Capastrum.Mcp
|  `- Capastrum.Mcp.Client
`- Capastrum.Acp.Protocol
   |- Capastrum.Acp
   `- Capastrum.Acp.Server
```

`Capastrum.JsonRpc` отвечает за конверт, сопоставление запросов и ответов, отмену и кадрирование потока. Протокольные пакеты превращают параметры JSON-RPC в типизированные записи и обратно через source-generated `JsonSerializerContext`. Пакеты сервера, клиента, хоста и агента содержат только поведение протокола: они не запускают контейнер зависимостей, не настраивают логирование и не владеют жизненным циклом приложения.

MCP реализован как stateless-ядро: `server/discover` согласует версию и возможности, `_meta` несёт метаданные протокола, результаты инструментов используют `resultType`, а многораундовые запросы выражаются результатами `input_required`. ACP использует модель stdio версии v1 с явными запросами разрешений, поэтому хост решает, что агенту разрешено выполнять и записывать.

Полное описание - в [docs/ru/architecture.md](docs/ru/architecture.md).

---

## Сборка и тестирование

```sh
dotnet build Capastrum.sln -c Release
dotnet test Capastrum.sln -c Release
dotnet pack Capastrum.sln -c Release -o artifacts/packages
pwsh ./eng/verify-packages.ps1
```

Запуск бенчмарков:

```sh
dotnet run -c Release --project benchmarks/Capastrum.Benchmarks -- --filter "*"
```

CI собирает, тестирует, упаковывает и проверяет все семь пакетов при каждом push и pull request.

Процесс для контрибьюторов, ветки и соглашения о коммитах: [CONTRIBUTING.md](CONTRIBUTING.md).

---

## Требования

- .NET SDK 10; репозиторий фиксирует `10.0.401` в `global.json`.
- У `Capastrum.JsonRpc` нет runtime-зависимостей вне базовой библиотеки классов; каждый остальной пакет ссылается только на пакет уровнем ниже.

---

## Документация

Вся документация - в **[docs/](docs/README.md)**, на английском и русском.

| Документ | Языки | Описание |
| --- | --- | --- |
| [README.md](README.md) - [ru](README.ru.md) | EN - RU | Обзор проекта и быстрый старт |
| [Ядро JSON-RPC](docs/ru/json-rpc.md) - [en](docs/en/json-rpc.md) | RU - EN | Конверт, peer, транспорты, ошибки, производительность |
| [MCP](docs/ru/mcp.md) - [en](docs/en/mcp.md) | RU - EN | Сервер, клиент, MRTR, подписки, HTTP |
| [ACP](docs/ru/acp.md) - [en](docs/en/acp.md) | RU - EN | Хост, агент, разрешения, файлы, терминалы |
| [Архитектура](docs/ru/architecture.md) - [en](docs/en/architecture.md) | RU - EN | Граф пакетов, слои протоколов, проектные решения |

---

## Лицензия

MIT (c) 2026 FrigaT
