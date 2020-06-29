using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NSubstitute;
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

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference}, expected);
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

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference});
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
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 11, column: 18)}
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference}, expected);
        }

        [Fact]
        public Task NSubstituteExtensionsReceivedWithoutExpectedCallCountIsBannedAsync()
        {
            const string test = @"
     using System;
     using NSubstitute;

     namespace FunFair.FunWallet.Api.Logic.Tests.Services
     {
          public interface IDoSomething
          {
                 void DoIt();
          }
     }

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Substitute.For<IDoSomething>.Received().DoIt();
             }
         }
     }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0014",
                                            Message = @"Only use Received with expected call count",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 19, column: 40) }
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(Substitute).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference}, expected);
        }
    }
}