using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class InternalsVisibleToDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<InternalsVisibleToDiagnosticsAnalyzer>
{
    [Fact]
    public Task UsingInternalsVisibleToIsErrorAsync()
    {
        const string test =
            @"using System.Runtime.CompilerServices;
[assembly: InternalsVisibleTo(""SomeAssembly"")]";

        DiagnosticResult expected = Result(
            id: "FFS0051",
            message: "Do not use InternalsVisibleTo",
            severity: DiagnosticSeverity.Error,
            line: 2,
            column: 12
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task UsingInternalsVisibleToWithFullyQualifiedNameIsErrorAsync()
    {
        const string test = "[assembly: System.Runtime.CompilerServices.InternalsVisibleTo(\"SomeAssembly\")]";

        DiagnosticResult expected = Result(
            id: "FFS0051",
            message: "Do not use InternalsVisibleTo",
            severity: DiagnosticSeverity.Error,
            line: 1,
            column: 12
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task NotUsingInternalsVisibleToIsOkAsync()
    {
        const string test = "public sealed class Test { }";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}
