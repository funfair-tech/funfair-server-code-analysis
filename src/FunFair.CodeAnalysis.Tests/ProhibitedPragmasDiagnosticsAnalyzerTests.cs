using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedPragmasDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedPragmasDiagnosticsAnalyzer();
        }

        [Fact]
        public Task AllowedWarningIsNotAnErrorAsync()
        {
            const string test = @"#pragma warning disable 1591";

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task BannedWarningCannotBeDisabledAsync()
        {
            const string test = @"#pragma warning disable 1234";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0008",
                                            Message = "Don't disable warnings using #pragma warning disable",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
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

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0008",
                                            Message = "Don't disable warnings using #pragma warning disable",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 37)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task NoErrorsReportedAsync()
        {
            const string test = @"";

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task RestoringBannedWarningIsNotAnErrorAsync()
        {
            const string test = @"#pragma warning restore 1234";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0008",
                                            Message = "Don't disable warnings using #pragma warning disable",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }
    }
}