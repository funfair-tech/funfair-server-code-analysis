using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ForcedMethodParametersInvocationsDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<ForceMethodParametersInvocationsDiagnosticsAnalyzer>
{
    [Fact]
    public Task DeserializerWithJsonSerializerOptionsIsAllowedAsync()
    {
        const string test =
            @"
    using System.Text.Json;
    using System.Text.Json.Serialization;

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
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                Test test = JsonSerializer.Deserialize<Test>(""{'Id': 108}"", jsonSerializerOptions);
            }
        }
    }";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.JsonSerializer);
    }

    [Fact]
    public Task DeserializerWithoutJsonSerializerOptionsIsNotAllowedAsync()
    {
        const string test =
            @"
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

        DiagnosticResult expected = Result(id: "FFS0015", message: "Only use JsonSerializer.Deserialize with own JsonSerializerOptions", severity: DiagnosticSeverity.Error, line: 15, column: 29);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.JsonSerializer, expected: expected);
    }

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

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Substitute);
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

        DiagnosticResult expected = Result(id: "FFS0018", message: "Only use Received with expected call count", severity: DiagnosticSeverity.Error, line: 15, column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Substitute, expected: expected);
    }

    [Fact]
    public Task SerializerWithJsonSerializerOptionsIsAllowedAsync()
    {
        const string test =
            @"
    using System.Text.Json;
    using System.Text.Json.Serialization

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
                JsonSerializerOptions jsonSerializerOptions = new JsonSerializerOptions {DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, PropertyNamingPolicy = JsonNamingPolicy.CamelCase};
                string content = JsonSerializer.Serialize(new Test { Id = 108 }, jsonSerializerOptions);
            }
        }
    }";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.JsonSerializer);
    }

    [Fact]
    public Task SerializerWithoutJsonSerializerOptionsIsNotAllowedAsync()
    {
        const string test =
            @"
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

        DiagnosticResult expected = Result(id: "FFS0014", message: "Only use JsonSerializer.Serialize with own JsonSerializerOptions", severity: DiagnosticSeverity.Error, line: 15, column: 34);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.JsonSerializer, expected: expected);
    }
}
