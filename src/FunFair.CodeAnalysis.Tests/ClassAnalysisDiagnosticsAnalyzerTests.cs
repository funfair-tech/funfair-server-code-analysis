using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ClassAnalysisDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<ClassAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task AbstractClassNotAnErrorAsync()
    {
        const string test = "public abstract class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task ClassWithNoModifiersIsAnErrorAsync()
    {
        const string test = "public class Test {}";
        DiagnosticResult expected = Result(id: "FFS0012", message: "Classes should be static, sealed or abstract", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task SealedClassNotAnErrorAsync()
    {
        const string test = "public sealed class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task StaticClassNotAnErrorAsync()
    {
        const string test = "public static class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}