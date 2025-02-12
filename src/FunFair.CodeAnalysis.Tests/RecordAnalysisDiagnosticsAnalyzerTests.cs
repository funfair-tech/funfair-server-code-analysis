using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class RecordAnalysisDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<RecordAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task RecordWithNoModifiersIsAnErrorAsync()
    {
        const string test = "public record Test {}";
        DiagnosticResult expected = Result(id: "FFS0028", message: "Records should be sealed", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task SealedRecordNotAnErrorAsync()
    {
        const string test = "public sealed record Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}
