using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ClassAnalysisDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ClassAnalysisDiagnosticsAnalyzer();
        }

        [Fact]
        public Task AbstractClassNotAnErrorAsync()
        {
            const string test = @"public abstract class Test {}";

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task ClassWithNoModifiersIsAnErrorAsync()
        {
            const string test = @"public class Test {}";
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0012",
                                            Message = "Classes should be static, sealed or abstract",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task SealedClassNotAnErrorAsync()
        {
            const string test = @"public sealed class Test {}";

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task StaticClassNotAnErrorAsync()
        {
            const string test = @"public static class Test {}";

            return this.VerifyCSharpDiagnosticAsync(test);
        }
    }
}