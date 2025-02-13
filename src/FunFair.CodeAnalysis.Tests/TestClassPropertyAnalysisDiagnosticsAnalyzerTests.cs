using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class TestClassPropertyAnalysisDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<TestClassPropertyAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task NonTestClassAllowedMutablePropertiesAsync()
    {
        const string test =
            @"
public sealed class NormalClass {
    public int TestValue {get; set;}

}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task TestClassProhibitedMutablePropertyAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {get; set;}

}";

        DiagnosticResult expected = Result(
            id: "FFS0036",
            message: "Properties in test classes should be read-only or const",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 5
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon],
            expected: expected
        );
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    Test() {
        Value = 42;
    }

    public int Value {get;}

}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithLambdaAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value => 42;

}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithCompileTimeAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {get;} = 42;

}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithExplicitBodyAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {
            get
            {
                return 42;
            }
        }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task TestClassAllowedInitOnlyPropertyAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    Test() {
        Value = 42;
    }

    public int Value {get; set;}

}";

        DiagnosticResult expected = Result(
            id: "FFS0036",
            message: "Properties in test classes should be read-only or const",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 5
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon],
            expected: expected
        );
    }

    [Fact]
    public Task TestClassProhibitedMutablePropertyWithExplicitBodyAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;

public sealed class Test : TestBase {
    private int _value;
    Test() {
        _value = 42;
    }

    public int Value {
            get
            {
                return _value;
            }
            set
            {
                _value = value;
            }
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0036",
            message: "Properties in test classes should be read-only or const",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 5
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon],
            expected: expected
        );
    }
}
