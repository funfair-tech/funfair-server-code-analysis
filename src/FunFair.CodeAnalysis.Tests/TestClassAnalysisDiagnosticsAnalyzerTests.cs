using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class TestClassAnalysisDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<TestClassAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task ClassThatHasNothingToDoWithTestsIsNotAnErrorAsync()
    {
        const string test =
            @"
            public sealed class Test {

            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task FactClassThatDoesNotDeriveFromTestBaseIsAnErrorAsync()
    {
        const string test =
            @"
using Xunit;

            public sealed class Test {

            [Fact]
            public void DoIt()
            {
            }
}";
        DiagnosticResult expected = Result(
            id: "FFS0013",
            message: "Test classes should be derived from TestBase",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 13
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Xunit,
            expected: expected
        );
    }

    [Fact]
    public Task FactClassThatInheritsFromLoggingTestBaseIsNotAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : LoggingTestBase {

            public Test(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.XunitAbstractions,
            ]
        );
    }

    [Fact]
    public Task FactClassThatInheritsFromTestBaseIsNotAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : TestBase {

            [Fact]
            public void DoIt()
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task TheoryClassThatDoesNotDeriveFromTestBaseIsAnErrorAsync()
    {
        const string test =
            @"
using Xunit;

            public sealed class Test {

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";
        DiagnosticResult expected = Result(
            id: "FFS0013",
            message: "Test classes should be derived from TestBase",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 13
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit],
            expected: expected
        );
    }

    [Fact]
    public Task TheoryClassThatInheritsFromLoggingTestBaseIsNotAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : LoggingTestBase {

            public Test(ITestOutputHelper output)
                : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.XunitAbstractions,
            ]
        );
    }

    [Fact]
    public Task TheoryClassThatInheritsFromTestBaseIsNotAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : TestBase {

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }
}
