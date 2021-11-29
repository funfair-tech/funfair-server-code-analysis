using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ForcedMethodParametersInvocationsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ForceMethodParametersInvocationsDiagnosticsAnalyzer();
        }

        [Fact]
        public Task DeserializerWithJsonSerializerOptionsIsAllowedAsync()
        {
            const string test = @"
    using System.Text.Json;

    namespace ConsoleApplication1
    {
        class Test
        {
            public int Id { get; set; }
        }

        class TypeName
        {
            void Test()
            {
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                Test test = JsonSerializer.Deserialize<Test>(""{'Id': 108}"", jsonSerializerOptions);
            }
        }
    }";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.JsonSerializer
                                                    });
        }

        [Fact]
        public Task DeserializerWithoutJsonSerializerOptionsIsNotAllowedAsync()
        {
            const string test = @"
    using System.Text.Json;

    namespace ConsoleApplication1
    {
        class Test
        {
            public int Id { get; set; }
        }

        class TypeName
        {
            void Test()
            {
                Test test = JsonSerializer.Deserialize<Test>(""{'Id': 108}"");
            }
        }
    }";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0015",
                                            Message = @"Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 29)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.JsonSerializer
                                                    },
                                                    expected);
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
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.Substitute
                                                    });
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0018",
                                            Message = @"Only use Received with expected call count",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 18)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.Substitute
                                                    },
                                                    expected);
        }

        [Fact]
        public Task SerializerWithJsonSerializerOptionsIsAllowedAsync()
        {
            const string test = @"
    using System.Text.Json;

    namespace ConsoleApplication1
    {
        class Test
        {
            public int Id { get; set; }
        }

        class TypeName
        {
            void Test()
            {
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {IgnoreNullValues = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                string content = JsonSerializer.Serialize(new Test { Id = 108 }, jsonSerializerOptions);
            }
        }
    }";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.JsonSerializer
                                                    });
        }

        [Fact]
        public Task SerializerWithoutJsonSerializerOptionsIsNotAllowedAsync()
        {
            const string test = @"
    using System.Text.Json;

    namespace ConsoleApplication1
    {
        class Test
        {
            public int Id { get; set; }
        }

        class TypeName
        {
            void Test()
            {
                string content = JsonSerializer.Serialize(new Test { Id = 108 });
            }
        }
    }";
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0014",
                                            Message = @"Only use JsonSerializer.Serialize with own JsonSerializerOptions",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 34)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.JsonSerializer
                                                    },
                                                    expected);
        }
    }
}