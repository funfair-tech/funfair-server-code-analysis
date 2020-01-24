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
        public void AllowedWarningIsNotAnError()
        {
            const string test = @"#pragma warning disable 8618";
            this.VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void BannedWarningCannotBeDisabled()
        {
            const string test = @"#pragma warning disable 1234";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0008",
                                            Message = "Don't disable warnings using #pragma warning disable",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void NoErrorsReported()
        {
            const string test = @"";

            this.VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void RestoringBannedWarningIsNotAnError()
        {
            const string test = @"#pragma warning restore 1234";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0008",
                                            Message = "Don't disable warnings using #pragma warning disable",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };
            this.VerifyCSharpDiagnostic(test, expected);
        }
    }
}