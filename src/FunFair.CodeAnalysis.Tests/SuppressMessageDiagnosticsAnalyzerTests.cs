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
    public Task SuppressMessageWithJustificationIsErrorAsync()
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

        DiagnosticResult expected = Result(
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'ExampleCheckId'",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
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
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'ExampleCheckId'",
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
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'ExampleCheckId'",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 14
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
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'ExampleCheckId'",
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
    public Task AllowedNx0001SuppressMessageWithJustificationIsOkAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Nullable.Extended.Analyzer"", checkId: ""NX0001: Suppression of NullForgiving operator is not required"", Justification = ""Required here"")]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage
        );
    }

    [Fact]
    public Task AllowedSuppressMessageWithJustificationIsOkAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231"", Justification = ""params ReadOnlySpan cannot be ref read-only"")]
            public static void DoIt(params System.ReadOnlySpan<string> items)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage
        );
    }

    [Fact]
    public Task AllowedSuppressMessageWithFullCheckIdWithJustificationIsOkAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231: Spans should be ref read-only"", Justification = ""Except when they're in a params parameter"")]
            public static void DoIt(params System.ReadOnlySpan<string> items)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage
        );
    }

    [Fact]
    public Task AllowedSuppressMessageWithoutJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231"")]
            public static void DoIt(params System.ReadOnlySpan<string> items)
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
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task AllowedSuppressMessageWithBlankJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231"", Justification = "" "")]
            public static void DoIt(params System.ReadOnlySpan<string> items)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0027",
            message: "SuppressMessage must specify a Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 100
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task AllowedSuppressMessageWithTodoJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231"", Justification = ""TODO: Fix later"")]
            public static void DoIt(params System.ReadOnlySpan<string> items)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0042",
            message: "SuppressMessage must not have a TODO Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 100
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task SuppressMessageForRCS1231OnNonParamsMethodIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(category: ""Roslynator.Analyzers"", checkId: ""RCS1231"", Justification = ""Because"")]
            public static void DoIt(System.ReadOnlySpan<string> items)
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'RCS1231'",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }
}
