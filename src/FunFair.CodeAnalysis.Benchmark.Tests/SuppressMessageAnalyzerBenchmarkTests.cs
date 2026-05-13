using BenchmarkDotNet.Loggers;
using BenchmarkDotNet.Reports;
using FunFair.CodeAnalysis.Benchmark.Tests.Bench;
using FunFair.Test.Common;
using Xunit;

namespace FunFair.CodeAnalysis.Benchmark.Tests;

public sealed class SuppressMessageAnalyzerBenchmarkTests : LoggingTestBase
{
    public SuppressMessageAnalyzerBenchmarkTests(ITestOutputHelper output)
        : base(output) { }

    [Fact]
    public void Run_Benchmarks()
    {
        (Summary summary, AccumulationLogger logger) = Benchmark<SuppressMessageAnalyzerBenchmark>();

        this.Output.WriteLine(logger.GetLog());

        // Baseline measured on net9.0 Release: NoSuppression=560B, AllowedSuppression=560B, DisallowedSuppression=592B
        summary.AssertAllocationsAtMost(benchmarkName: "NoSuppressionAsync", maximumBytes: 1024);
        summary.AssertAllocationsAtMost(benchmarkName: "AllowedSuppressionAsync", maximumBytes: 1024);
        summary.AssertAllocationsAtMost(benchmarkName: "DisallowedSuppressionAsync", maximumBytes: 1024);
    }
}
