using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class FileNameMustMatchTypeNameDiagnosticsAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new FileNameMustMatchTypeNameDiagnosticsAnalyzer();
    }

    [Fact]
    public Task ClassNameMatchesFileNameIsOkAsync()
    {
        const string test = "public sealed class Test0 {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RecordNameMatchesFileNameIsOkAsync()
    {
        const string test = "public sealed record Test0 {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task EnumMatchesFileNameIsOkAsync()
    {
        const string test = "public enum Test0 {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task InterfaceMatchesFileNameIsOkAsync()
    {
        const string test = "public interface Test0 {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task StructMatchesFileNameIsOkAsync()
    {
        const string test = "public readonly struct Test0 {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task ClassNameDoesNotMatchFileNameIsAnErrorAsync()
    {
        const string test = "public sealed class Example {}";

        DiagnosticResult expected = Result(id: "FFS0040", message: "Should be in a file of the same name as the type", severity: DiagnosticSeverity.Error, line: 1, column: 1);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task RecordNameDoesNotMatchFileNameIsAnErrorAsync()
    {
        const string test = "public sealed record Example {}";

        DiagnosticResult expected = Result(id: "FFS0040", message: "Should be in a file of the same name as the type", severity: DiagnosticSeverity.Error, line: 1, column: 1);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task EnumNameDoesNotMatchFileNameIsAnErrorAsync()
    {
        const string test = "public enum Example {}";

        DiagnosticResult expected = Result(id: "FFS0040", message: "Should be in a file of the same name as the type", severity: DiagnosticSeverity.Error, line: 1, column: 1);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task StructNameDoesNotMatchFileNameIsAnErrorAsync()
    {
        const string test = "public readonly struct Example {}";

        DiagnosticResult expected = Result(id: "FFS0040", message: "Should be in a file of the same name as the type", severity: DiagnosticSeverity.Error, line: 1, column: 1);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task InterfaceNameDoesNotMatchFileNameIsAnErrorAsync()
    {
        const string test = "public interface Example {}";

        DiagnosticResult expected = Result(id: "FFS0040", message: "Should be in a file of the same name as the type", severity: DiagnosticSeverity.Error, line: 1, column: 1);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }
}