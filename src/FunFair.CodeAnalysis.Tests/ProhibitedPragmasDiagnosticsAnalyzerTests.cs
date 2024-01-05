using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedPragmasDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<ProhibitedPragmasDiagnosticsAnalyzer>
{
    [Fact]
    public Task AllowedWarningIsNotAnErrorAsync()
    {
        const string test = "#pragma warning disable 1591";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task BannedWarningCannotBeDisabledAsync()
    {
        const string test = "#pragma warning disable 1234";
        DiagnosticResult expected = Result(id: "FFS0008", message: "Don't disable warnings using #pragma warning disable", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task BannedWarningCannotBeDisabledInMethodAsync()
    {
        const string test = @"

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void DoIt()
            {
            #pragma warning disable CS1234
            }
        }
    }";

        DiagnosticResult expected = Result(id: "FFS0008", message: "Don't disable warnings using #pragma warning disable", severity: DiagnosticSeverity.Error, line: 9, column: 37);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task NoErrorsReportedAsync()
    {
        const string test = "";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RestoringBannedWarningIsNotAnErrorAsync()
    {
        const string test = "#pragma warning restore 1234";
        DiagnosticResult expected = Result(id: "FFS0008", message: "Don't disable warnings using #pragma warning disable", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }
}