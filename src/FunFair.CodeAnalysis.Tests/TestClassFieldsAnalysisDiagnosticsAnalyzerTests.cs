using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class TestClassFieldsAnalysisDiagnosticsAnalyzerTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new TestClassFieldsAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task NonTestClassAllowedMutableFieldsAsync()
    {
        const string test = @"
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
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private int _test;

    public void Exec()
    {
        ++_test;
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0035",
                                        Message = "Fields in test classes should be read-only or const",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 5, column: 5) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon }, expected);
    }

    [Fact]
    public Task TestClassAllowedReadOnlyFieldAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private readonly int _test = 42;

    public int Value()
    {
        return _test;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }

    [Fact]
    public Task TestClassAllowedConstFieldAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase{
    private const int _test = 42;

    public int Value()
    {
        return _test;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }
}