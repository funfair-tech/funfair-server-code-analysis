using System.Text.Json;
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

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(JsonSerializer).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference});
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

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0015",
                                            Message = @"Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 29)}
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(JsonSerializer).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference}, expected);
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

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(JsonSerializer).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference});
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
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0014",
                                            Message = @"Only use JsonSerializer.Serialize with own JsonSerializerOptions",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 15, column: 34)}
                                        };

            MetadataReference reference = MetadataReference.CreateFromFile(typeof(JsonSerializer).Assembly.Location);

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {reference}, expected);
        }
    }
}