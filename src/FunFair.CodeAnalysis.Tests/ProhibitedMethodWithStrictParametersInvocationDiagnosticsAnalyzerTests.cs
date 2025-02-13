using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzer>
{
    [Fact]
    public Task NSubstituteExtensionsReceivedWithExpectedCallCountIsPassingAsync()
    {
        const string test =
            @"
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

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Substitute
        );
    }

    [Fact]
    public Task NSubstituteExtensionsReceivedWithZeroExpectedCallCountIsFailingAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0021",
            message: "Only use Received with expected call count greater than 0, use DidNotReceived instead if 0 call received expected",
            severity: DiagnosticSeverity.Error,
            line: 15,
            column: 18
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Substitute,
            expected: expected
        );
    }

    [Fact]
    public Task NSubstituteExtensionsReceivedWithoutExpectedCallCountIsBannedAsync()
    {
        const string test =
            @"
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

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Substitute
        );
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToFalseIsPassingAsync()
    {
        const string test =
            @"
     using Microsoft.Extensions.Configuration;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 IConfigurationBuilder builder = new ConfigurationBuilder().AddJsonFile(path: ""appsettings.json"", optional: true, reloadOnChange: false);
             }
         }
     }";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
                WellKnownMetadataReferences.ConfigurationBuilder,
                WellKnownMetadataReferences.JsonConfigurationExtensions,
            ]
        );
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToTrueIsBannedConstructorAsync()
    {
        const string test =
            @"
     using Microsoft.Extensions.Configuration;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 IConfigurationRoot builder = new ConfigurationBuilder()
                                    .AddJsonFile(path: ""appsettings.json"", optional: false, reloadOnChange: true)
                                    .Build();
           }
         }
     }";

        DiagnosticResult expected = Result(
            id: "FFS0034",
            message: "Only use AddJsonFile with reloadOnChange set to false",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 47
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
                WellKnownMetadataReferences.ConfigurationBuilder,
                WellKnownMetadataReferences.JsonConfigurationExtensions,
            ],
            expected: expected
        );
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToTrueIsBannedNonConstructorAsync()
    {
        const string test =
            @"
     using Microsoft.Extensions.Configuration;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 IConfigurationRoot builder = new ConfigurationBuilder()
                                    .AddJsonFile(path: ""appsettings.json"", optional: false, reloadOnChange: false)
                                    .AddJsonFile(path: ""appsettings1.json"", optional: true, reloadOnChange: true)
                                    .Build();
           }
         }
     }";

        DiagnosticResult expected = Result(
            id: "FFS0034",
            message: "Only use AddJsonFile with reloadOnChange set to false",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 47
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
                WellKnownMetadataReferences.ConfigurationBuilder,
                WellKnownMetadataReferences.JsonConfigurationExtensions,
            ],
            expected: expected
        );
    }
}
