using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class NullableDirectiveDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<NullableDirectiveDiagnosticsAnalyzer>
{
    [Fact]
    public Task NullableDisableIsAnErrorAsync()
    {
        const string test = "#nullable disable";

        DiagnosticResult expected = Result(id: "FFS0022",
                                           message: "Don't use #nulllable directive, make the change globally for the project",
                                           severity: DiagnosticSeverity.Error,
                                           line: 9,
                                           column: 37);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task NullableRestoreIsAnErrorAsync()
    {
        const string test = "#nullable restore";

        DiagnosticResult expected = Result(id: "FFS0022",
                                           message: "Don't use #nulllable directive, make the change globally for the project",
                                           severity: DiagnosticSeverity.Error,
                                           line: 9,
                                           column: 37);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task NullableEnableIsAnErrorAsync()
    {
        const string test = "#nullable enable";

        DiagnosticResult expected = Result(id: "FFS0022",
                                           message: "Don't use #nulllable directive, make the change globally for the project",
                                           severity: DiagnosticSeverity.Error,
                                           line: 9,
                                           column: 37);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }
}