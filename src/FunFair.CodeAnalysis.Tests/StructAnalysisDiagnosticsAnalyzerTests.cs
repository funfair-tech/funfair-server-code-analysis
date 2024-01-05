using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class StructAnalysisDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<StructAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task NonReadOnlyStructIsAnErrorAsync()
    {
        const string test = "public struct Test {}";
        DiagnosticResult expected = Result(id: "FFS0011", message: "Structs should be read-only", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task ReadOnlyStructNotAnErrorAsync()
    {
        const string test = "public readonly struct Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}