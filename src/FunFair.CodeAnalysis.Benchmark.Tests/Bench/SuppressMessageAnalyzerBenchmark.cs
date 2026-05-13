using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Benchmark.Tests.Bench;

[SimpleJob]
[MinColumn]
[MaxColumn]
[MeanColumn]
[MedianColumn]
[MemoryDiagnoser(false)]
[SuppressMessage(category: "FunFair.CodeAnalysis", checkId: "FFS0012:Make Sealed", Justification = "Benchmarks")]
public class SuppressMessageAnalyzerBenchmark
{
    private static readonly CancellationToken BenchmarkCancellationToken = new(canceled: false);

    private static readonly string? AssemblyDirectory = Path.GetDirectoryName(typeof(object).Assembly.Location);

    private static readonly ImmutableArray<MetadataReference> References =
    [
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(Path.Combine(AssemblyDirectory ?? string.Empty, path2: "System.Runtime.dll")),
        MetadataReference.CreateFromFile(Path.Combine(AssemblyDirectory ?? string.Empty, path2: "System.dll")),
    ];

    private CompilationWithAnalyzers? _allowedSuppression;
    private CompilationWithAnalyzers? _disallowedSuppression;
    private CompilationWithAnalyzers? _noSuppression;

    [GlobalSetup]
    public void Setup()
    {
        this._noSuppression = BuildCompilationWithAnalyzers(
            source: """
            public sealed class Example
            {
                public void Method() { }
            }
            """
        );

        this._allowedSuppression = BuildCompilationWithAnalyzers(
            source: """
            using System.Diagnostics.CodeAnalysis;

            public sealed class Example
            {
                [SuppressMessage("Nullable.Extended.Analyzer", "NX0001: Suppression of NullForgiving operator is not required", Justification = "Required here")]
                public void Method() { }
            }
            """
        );

        this._disallowedSuppression = BuildCompilationWithAnalyzers(
            source: """
            using System.Diagnostics.CodeAnalysis;

            public sealed class Example
            {
                [SuppressMessage("Example", "EX0001: Some check", Justification = "Because")]
                public void Method() { }
            }
            """
        );
    }

    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> NoSuppressionAsync()
    {
        return GetOrThrow(this._noSuppression).GetAnalyzerDiagnosticsAsync(BenchmarkCancellationToken);
    }

    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> AllowedSuppressionAsync()
    {
        return GetOrThrow(this._allowedSuppression).GetAnalyzerDiagnosticsAsync(BenchmarkCancellationToken);
    }

    [Benchmark]
    public Task<ImmutableArray<Diagnostic>> DisallowedSuppressionAsync()
    {
        return GetOrThrow(this._disallowedSuppression).GetAnalyzerDiagnosticsAsync(BenchmarkCancellationToken);
    }

    private static CompilationWithAnalyzers GetOrThrow(CompilationWithAnalyzers? value)
    {
        return value ?? throw new InvalidOperationException("Benchmark not initialised. Call Setup() first.");
    }

    private static CompilationWithAnalyzers BuildCompilationWithAnalyzers(string source)
    {
        CSharpCompilation compilation = CSharpCompilation.Create(
            assemblyName: "BenchmarkSource",
            syntaxTrees: [CSharpSyntaxTree.ParseText(text: source, cancellationToken: BenchmarkCancellationToken)],
            references: References,
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        return compilation.WithAnalyzers([new SuppressMessageDiagnosticsAnalyzer()]);
    }
}
