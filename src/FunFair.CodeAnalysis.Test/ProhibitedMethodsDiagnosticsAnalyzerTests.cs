using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        // protected override CodeFixProvider GetCSharpCodeFixProvider()
        // {
        //     return new ProhibitedMethodsCodeFixProvider();
        // }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodsDiagnosticsAnalyzer();
        }

        //Diagnostic and CodeFix both triggered and checked for
        [Fact]
        public void DateTimeNowIsBanned()
        {
            const string test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTime.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 28)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeOffsetNowIsBanned()
        {
            const string test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTimeOffset.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0002",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 28)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        //No diagnostics expected to show up
        [Fact]
        public void NoErrorsReported()
        {
            string test = @"";

            this.VerifyCSharpDiagnostic(test);
        }
    }
}