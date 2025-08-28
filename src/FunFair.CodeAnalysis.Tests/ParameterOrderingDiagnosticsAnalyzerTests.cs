using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ParameterOrderingDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ParameterOrderingDiagnosticsAnalyzer>
{
    [Fact]
    public Task GenericLoggerAsLastParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(string banana, ILogger<Test> logger)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task GenericLoggerAsOnlyParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task GenericLoggerParameterShouldBeLastWhenNoCancellationTokenAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.GenericLogger,
            expected: expected
        );
    }

    [Fact]
    public Task GenericLoggerParameterShouldBeNextLastWhenCancellationTokenAsync()
    {
        const string test =
            @"
            using System.Threading;
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger<Test> logger, string banana, CancellationToken cancellationToken)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.GenericLogger, WellKnownMetadataReferences.CancellationToken],
            expected: expected
        );
    }

    [Fact]
    public Task LoggerAsLastParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(string banana, ILogger logger)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task LoggerAsOnlyParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task LoggerExtensionMethodIsValidAsFirstParameterAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public static class Test {

            public static void DoIt(this ILogger logger, string banana)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task LoggerParameterShouldBeLastWhenNoCancellationTokenAsync()
    {
        const string test =
            @"
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger, string banana)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.GenericLogger,
            expected: expected
        );
    }

    [Fact]
    public Task LoggerParameterShouldBeNextLastWhenCancellationTokenAsync()
    {
        const string test =
            @"
            using System.Threading;
            using Microsoft.Extensions.Logging;

            public sealed class Test {

            public void DoIt(ILogger logger, string banana, CancellationToken cancellationToken)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.GenericLogger, WellKnownMetadataReferences.CancellationToken],
            expected: expected
        );
    }
}
