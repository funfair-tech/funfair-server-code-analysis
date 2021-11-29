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
        public Task GenericLoggerAsLastParameterIsNotAnErrorAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(string banana, ILogger<Test> logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    });
        }

        [Fact]
        public Task GenericLoggerAsOnlyParameterIsNotAnErrorAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    });
        }

        [Fact]
        public Task GenericLoggerParameterShouldBeLastWhenNoCancellationTokenAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana)
            {
            }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 30)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    },
                                                    expected);
        }

        [Fact]
        public Task GenericLoggerParameterShouldBeNextLastWhenCancellationTokenAsync()
        {
            const string test = @"
            using System.Threading;
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana, CancellationToken cancellationToken)
            {
            }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 7, column: 30)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger,
                                                        WellKnownMetadataReferences.CancellationToken
                                                    },
                                                    expected);
        }

        [Fact]
        public Task LoggerAsLastParameterIsNotAnErrorAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(string banana, ILogger logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    });
        }

        [Fact]
        public Task LoggerAsOnlyParameterIsNotAnErrorAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    });
        }

        [Fact]
        public Task LoggerExtensionMethodIsValidAsFirstParameterAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public static class Test {

            public static void DoIt(this ILogger logger, string banana)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    });
        }

        [Fact]
        public Task LoggerParameterShouldBeLastWhenNoCancellationTokenAsync()
        {
            const string test = @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger, string banana)
            {
            }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 30)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger
                                                    },
                                                    expected);
        }

        [Fact]
        public Task LoggerParameterShouldBeNextLastWhenCancellationTokenAsync()
        {
            const string test = @"
            using System.Threading;
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger, string banana, CancellationToken cancellationToken)
            {
            }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0020",
                                            Message = "Parameter 'logger' must be parameter 2",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[]
                                                        {
                                                            new DiagnosticResultLocation(path: "Test0.cs", line: 7, column: 30)
                                                        }
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.GenericLogger,
                                                        WellKnownMetadataReferences.CancellationToken
                                                    },
                                                    expected);
        }
    }
}