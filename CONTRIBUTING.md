# Contributing to Capastrum

Thanks for your interest in improving Capastrum. This guide covers the build, the conventions and the pull request workflow.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/); the repository pins `10.0.401` in `global.json`.
- A recent IDE such as Visual Studio 2026, Rider, or VS Code with the C# extension.

## Build and test

```sh
dotnet restore Capastrum.sln
dotnet build Capastrum.sln -c Release
dotnet test Capastrum.sln -c Release
dotnet pack Capastrum.sln -c Release -o artifacts/packages
pwsh ./eng/verify-packages.ps1
```

Run the benchmarks:

```sh
dotnet run -c Release --project benchmarks/Capastrum.Benchmarks -- --filter "*"
```

The build treats warnings as errors and enforces nullable analysis and code style. A change is ready when the build, every test project and the packaging check pass.

## Workflow

1. Fork the repository and create a short-lived branch off `main`.
2. Add or update tests with the change.
3. Make sure the build and tests pass locally.
4. Open a pull request against `main` and describe what changed and how it was verified.

Write commit messages as one short, natural sentence in plain English, for example `Keep the menu inside the window`. Do not use Conventional Commit prefixes or scopes.

## Code conventions

- Every public type and member carries a meaningful `<summary>`; the documentation file is generated and checked by the build.
- Comments explain non-obvious decisions, not syntax. Prefer a clear name or a smaller method over a comment.
- Wire contracts use `System.Text.Json` source generation. Protocol paths must not depend on reflection-based serialization.
- Keep the package graph acyclic: a package references only the layer directly beneath it, and MCP and ACP never reference each other.
- Application-defined payloads stay `JsonElement`; do not invent a typed shape the specification does not define.
- New protocol behavior needs a contract test; changes that cross MCP and ACP belong in `Capastrum.Conformance.Tests`.
- Do not add a dependency that a package does not need. `Capastrum.JsonRpc` has no dependency outside the base class library.

## Reporting bugs and requesting features

Use the GitHub issue tracker. For security issues, read [SECURITY.md](SECURITY.md) and report privately instead of opening a public issue.

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
