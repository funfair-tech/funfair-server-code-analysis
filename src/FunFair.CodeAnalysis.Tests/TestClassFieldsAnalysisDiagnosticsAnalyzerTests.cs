using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class TestClassFieldsAnalysisDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<TestClassFieldsAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task NonTestClassAllowedMutableFieldsAsync()
    {
        const string test =
            @"
public sealed class NormalClass {
    private int _test;

    public void Exec()
    {
        ++_test;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task TestClassProhibitedMutableFieldsAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private int _test;

    public void Exec()
    {
        ++_test;
    }
}";

        DiagnosticResult expected = Result(id: "FFS0035", message: "Fields in test classes should be read-only or const", severity: DiagnosticSeverity.Error, line: 5, column: 5);

        return this.VerifyCSharpDiagnosticAsync(source: test, [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon], expected: expected);
    }

    [Fact]
    public Task TestClassAllowedReadOnlyFieldAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private readonly int _test = 42;

    public int Value()
    {
        return _test;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]);
    }

    [Fact]
    public Task TestClassAllowedConstFieldAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private const int _test = 42;

    public int Value()
    {
        return _test;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]);
    }
}
