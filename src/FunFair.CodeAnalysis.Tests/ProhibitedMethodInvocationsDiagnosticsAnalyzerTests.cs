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
     using System;
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

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task AssertFalseWithoutMessageIsBannedAsync()
        {
            const string test = @"
    using System;
    using Xunit;

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                Xunit.Assert.False(false);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0010",
                                            Message = @"Only use Assert.False with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 17)}
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(test, new[] {reference}, expected);
        }

        [Fact]
        public Task AssertTrueWithMessageIsAllowedAsync()
        {
            const string test = @"
    using System;
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

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(test, new[] {reference});
        }

        [Fact]
        public Task AssertTrueWithoutMessageIsBannedAsync()
        {
            const string test = @"
     using System;
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
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 31, column: 17)}
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(test, new[] {reference}, expected);
        }
    }
}