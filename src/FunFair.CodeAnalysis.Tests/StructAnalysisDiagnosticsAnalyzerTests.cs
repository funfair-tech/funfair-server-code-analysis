using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class StructAnalysisDiagnosticsAnalyzerTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new StructAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task NonReadOnlyStructIsAnErrorAsync()
    {
        const string test = @"public struct Test {}";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0011",
                                        Message = "Structs should be read-only",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ReadOnlyStructNotAnErrorAsync()
    {
        const string test = @"public readonly struct Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}