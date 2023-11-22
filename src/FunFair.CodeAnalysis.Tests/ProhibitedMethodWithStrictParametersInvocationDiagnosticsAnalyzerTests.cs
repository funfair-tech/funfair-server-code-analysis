using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzerTests : DiagnosticVerifier
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

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Substitute
        ]);
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
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 18)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Substitute
                                                ],
                                                expected);
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

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Substitute
        ]);
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToFalseIsPassingAsync()
    {
        const string test = @"
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

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
            WellKnownMetadataReferences.ConfigurationBuilder,
            WellKnownMetadataReferences.JsonConfigurationExtensions
        ]);
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToTrueIsBannedConstructorAsync()
    {
        const string test = @"
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

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0034",
                                        Message = "Only use AddJsonFile with reloadOnChange set to false",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 47)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
                                                    WellKnownMetadataReferences.ConfigurationBuilder,
                                                    WellKnownMetadataReferences.JsonConfigurationExtensions
                                                ],
                                                expected);
    }

    [Fact]
    public Task AddJsonFileWithReloadOnChangeSetToTrueIsBannedNonConstructorAsync()
    {
        const string test = @"
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

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0034",
                                        Message = "Only use AddJsonFile with reloadOnChange set to false",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 47)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.MicrosoftExtensionsIConfigurationBuilder,
                                                    WellKnownMetadataReferences.ConfigurationBuilder,
                                                    WellKnownMetadataReferences.JsonConfigurationExtensions
                                                ],
                                                expected);
    }
}