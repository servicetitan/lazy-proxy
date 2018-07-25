using BenchmarkDotNet.Running;

namespace LazyProxy.Unity.Benchmarks
{
    // ReSharper disable once ClassNeverInstantiated.Global
    class Program
    {
        static void Main() => BenchmarkRunner.Run<UnityExtensionBenchmark>();
    }
}