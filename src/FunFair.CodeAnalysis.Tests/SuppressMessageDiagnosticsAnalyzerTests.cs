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

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
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

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
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

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
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

    [Theory]
    [InlineData("codecracker.CSharp", "CC0091:MarkMembersAsStatic")]
    [InlineData("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
    public Task BenchmarkMethodSuppressMessageOnBenchmarkMethodIsOkAsync(string category, string checkId)
    {
        string test =
            $@"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute {{ }}

            public sealed class Test {{

            [SuppressMessage(category: ""{category}"", checkId: ""{checkId}"", Justification = ""Benchmark"")]
            [Benchmark]
            public void DoIt()
            {{
            }}
}}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
    }

    [Theory]
    [InlineData("codecracker.CSharp", "CC0091:MarkMembersAsStatic")]
    [InlineData("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
    public Task BenchmarkMethodSuppressMessageOnNonBenchmarkMethodIsErrorAsync(string category, string checkId)
    {
        string test =
            $@"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {{

            [SuppressMessage(category: ""{category}"", checkId: ""{checkId}"", Justification = ""Benchmark"")]
            public void DoIt()
            {{
            }}
}}";

        DiagnosticResult expected = Result(
            id: "FFS0049",
            message: $"SuppressMessage is not permitted for '{checkId}'",
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

    [Theory]
    [InlineData("codecracker.CSharp", "CC0091:MarkMembersAsStatic", 117)]
    [InlineData("Microsoft.Performance", "CA1822:MarkMembersAsStatic", 120)]
    public Task BenchmarkMethodSuppressMessageWithBlankJustificationIsErrorAsync(
        string category,
        string checkId,
        int column
    )
    {
        string test =
            $@"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute {{ }}

            public sealed class Test {{

            [SuppressMessage(category: ""{category}"", checkId: ""{checkId}"", Justification = "" "")]
            [Benchmark]
            public void DoIt()
            {{
            }}
}}";

        DiagnosticResult expected = Result(
            id: "FFS0027",
            message: "SuppressMessage must specify a Justification",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: column
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Theory]
    [InlineData("codecracker.CSharp", "CC0091:MarkMembersAsStatic", 117)]
    [InlineData("Microsoft.Performance", "CA1822:MarkMembersAsStatic", 120)]
    public Task BenchmarkMethodSuppressMessageWithTodoJustificationIsErrorAsync(
        string category,
        string checkId,
        int column
    )
    {
        string test =
            $@"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute {{ }}

            public sealed class Test {{

            [SuppressMessage(category: ""{category}"", checkId: ""{checkId}"", Justification = ""TODO: Fix later"")]
            [Benchmark]
            public void DoIt()
            {{
            }}
}}";

        DiagnosticResult expected = Result(
            id: "FFS0042",
            message: "SuppressMessage must not have a TODO Justification",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: column
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task BenchmarkClassFFS0012SuppressMessageOnClassWithBenchmarkMethodIsOkAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute { }

            [SuppressMessage(category: ""FunFair.CodeAnalysis"", checkId: ""FFS0012: Make sealed static or abstract"", Justification = ""Benchmark"")]
            public class Test {

            [Benchmark]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.SuppressMessage);
    }

    [Fact]
    public Task BenchmarkClassFFS0012SuppressMessageOnClassWithoutBenchmarkMethodIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            [SuppressMessage(category: ""FunFair.CodeAnalysis"", checkId: ""FFS0012: Make sealed static or abstract"", Justification = ""Benchmark"")]
            public class Test {

            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0049",
            message: "SuppressMessage is not permitted for 'FFS0012: Make sealed static or abstract'",
            severity: DiagnosticSeverity.Error,
            line: 4,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task BenchmarkClassFFS0012SuppressMessageWithBlankJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute { }

            [SuppressMessage(category: ""FunFair.CodeAnalysis"", checkId: ""FFS0012: Make sealed static or abstract"", Justification = "" "")]
            public class Test {

            [Benchmark]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0027",
            message: "SuppressMessage must specify a Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 132
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }

    [Fact]
    public Task BenchmarkClassFFS0012SuppressMessageWithTodoJustificationIsErrorAsync()
    {
        const string test =
            @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class BenchmarkAttribute : System.Attribute { }

            [SuppressMessage(category: ""FunFair.CodeAnalysis"", checkId: ""FFS0012: Make sealed static or abstract"", Justification = ""TODO: Fix later"")]
            public class Test {

            [Benchmark]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = Result(
            id: "FFS0042",
            message: "SuppressMessage must not have a TODO Justification",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 132
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.SuppressMessage,
            expected: expected
        );
    }
}
