using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzer();
        }

        [Fact]
        public Task NSubstituteExtensionsReceivedWithExpectedCallCountIsPassingAsync()
        {
            const string test = @"
     using NSubstitute;

     namespace ConsoleApplication1
     {
         public interface IDoSomething
          {
                 void DoIt();
          }

         class TypeName
         {
             void Test()
             {
                 Substitute.For<IDoSomething>().Received(1).DoIt();
             }
         }
     }";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Substitute});
        }

        [Fact]
        public Task NSubstituteExtensionsReceivedWithZeroExpectedCallCountIsFailingAsync()
        {
            const string test = @"
     using NSubstitute;

     namespace ConsoleApplication1
     {
         public interface IDoSomething
          {
                 void DoIt();
          }

         class TypeName
         {
             void Test()
             {
                 Substitute.For<IDoSomething>().Received(0).DoIt();
             }
         }
     }";
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0021",
                                            Message = "Only use Received with expected call count greater than 0, use DidNotReceived instead if 0 call received expected",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Substitute}, expected);
        }

        [Fact]
        public Task NSubstituteExtensionsReceivedWithoutExpectedCallCountIsBannedAsync()
        {
            const string test = @"
     using NSubstitute;

     namespace ConsoleApplication1
     {
         public interface IDoSomething
          {
                 void DoIt();
          }

         class TypeName
         {
             void Test()
             {
                 Substitute.For<IDoSomething>().Received().DoIt();
             }
         }
     }";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Substitute});
        }
    }
}