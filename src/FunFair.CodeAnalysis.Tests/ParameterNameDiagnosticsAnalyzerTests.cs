using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ParameterNameDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ParameterNameDiagnosticsAnalyzer();
        }

        [Fact]
        public Task GenericLoggerParameterNameInvalidAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logging)
            {
            }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0019",
                                            Message = "ILogger parameters should be called 'logger'",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 30)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.GenericLogger}, expected);
        }

        [Fact]
        public Task GenericLoggerParameterNameIsValidAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.GenericLogger});
        }

        [Fact]
        public Task LoggerParameterNameInvalidAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logging)
            {
            }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0019",
                                            Message = "ILogger parameters should be called 'logger'",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 30)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Logger}, expected);
        }

        [Fact]
        public Task LoggerParameterNameIsValidAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Logger});
        }
    }
}