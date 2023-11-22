using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class SuppressMessageDiagnosticsAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new SuppressMessageDiagnosticsAnalyzer();
    }

    [Fact]
    public Task SuppressMessageWithJustificationIsOkAsync()
    {
        const string test = @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = ""Because I said so"")]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.SuppressMessage
        ]);
    }

    [Fact]
    public Task SuppressMessageWithoutJustificationIsErrorAsync()
    {
        const string test = @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"")]
            public void DoIt()
            {
            }
}";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0027",
                                        Message = "SuppressMessage must specify a Justification",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 14)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.SuppressMessage
                                                ],
                                                expected);
    }

    [Fact]
    public Task SuppressMessageWithBlankJustificationIsErrorAsync()
    {
        const string test = @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = "" "")]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0027",
                                        Message = "SuppressMessage must specify a Justification",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 75)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.SuppressMessage
                                                ],
                                                expected);
    }

    [Fact]
    public Task SuppressMessageWithTodoJustificationIsErrorAsync()
    {
        const string test = @"
            using System.Diagnostics.CodeAnalysis;

            public sealed class Test {

            [SuppressMessage(""Example"", ""ExampleCheckId"", Justification = ""TODO: Write some tests"")]
            public void DoIt()
            {
            }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0042",
                                        Message = "SuppressMessage must not have a TODO Justification",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 75)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.SuppressMessage
                                                ],
                                                expected);
    }
}