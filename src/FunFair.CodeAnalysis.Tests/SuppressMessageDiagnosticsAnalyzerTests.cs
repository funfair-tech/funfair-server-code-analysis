using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class SuppressMessageDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<SuppressMessageDiagnosticsAnalyzer>
{
    [Fact]
    public Task SuppressMessageWithJustificationIsOkAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = ""Because I said so"")]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
    }

    [Fact]
    public Task SuppressMessageWithoutJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"")]
            public void DoIt()
            {
            }
}";
        DiagnosticResult expected = Result(
            id: "FFS0027",
            message: "SuppressMessage must specify a Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.SuppressMessage],
            expected: expected
        );
    }

    [Fact]
    public Task SuppressMessageWithBlankJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = "" "")]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0027",
            message: "SuppressMessage must specify a Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 75
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task SuppressMessageWithTodoJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = ""TODO: Write some tests"")]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0042",
            message: "SuppressMessage must not have a TODO Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 75
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.SuppressMessage],
            expected: expected
        );
    }
}
