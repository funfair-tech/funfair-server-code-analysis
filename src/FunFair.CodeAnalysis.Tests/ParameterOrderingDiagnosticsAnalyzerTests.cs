using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ParameterOrderingDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ParameterOrderingDiagnosticsAnalyzer();
        }

        [Fact]
        public Task LoggerParameterShouldBeLastWhenNoCancellationTokenAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana)
            {
            }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 30)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.GenericLogger}, expected);
        }

        [Fact]
        public Task LoggerParameterShouldBeNextLastWhenCancellationTokenAsync()
        {
            const string test = @"
            using System.Threading;
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana, CancellationToken cancellationToken)
            {
            }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 7, column: 30)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.GenericLogger, WellKnownMetadataReferences.CancellationToken}, expected);
        }
    }
}