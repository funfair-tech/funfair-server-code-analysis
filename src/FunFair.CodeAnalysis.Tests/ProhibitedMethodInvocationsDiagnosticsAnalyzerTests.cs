using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodInvocationsDiagnosticsAnalyzer();
        }

        [Fact]
        public Task AssertFalseWithMessageIsAllowedAsync()
        {
            const string test = @"
     using Xunit;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Assert.False(false, ""Somevalue"");
             }
         }
     }";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert});
        }

        [Fact]
        public Task AssertFalseWithoutMessageIsBannedAsync()
        {
            const string test = @"
    using Xunit;

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                Assert.False(false);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0010",
                                            Message = @"Only use Assert.False with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 11, column: 17)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert}, expected);
        }

        [Fact]
        public Task AssertTrueWithMessageIsAllowedAsync()
        {
            const string test = @"
    using Xunit;

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                Assert.True(true, ""Somevalue"");
            }
        }
    }";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert});
        }

        [Fact]
        public Task AssertTrueWithoutMessageIsBannedAsync()
        {
            const string test = @"
     using Xunit;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Assert.True(1==1);
             }
         }
     }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0009",
                                            Message = @"Only use Assert.True with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert}, expected);
        }
    }
}