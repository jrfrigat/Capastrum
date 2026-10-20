using BenchmarkDotNet.Running;
using Capastrum.Benchmarks;

BenchmarkSwitcher.FromAssembly(typeof(JsonRpcEnvelopeBenchmarks).Assembly).Run(args);
