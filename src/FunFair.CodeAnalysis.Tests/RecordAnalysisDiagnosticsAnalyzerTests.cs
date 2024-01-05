using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class RecordAnalysisDiagnosticsAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new RecordAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task RecordWithNoModifiersIsAnErrorAsync()
    {
        const string test = "public record Test {}";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0028",
                                        Message = "Records should be sealed",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task SealedRecordNotAnErrorAsync()
    {
        const string test = "public sealed record Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}