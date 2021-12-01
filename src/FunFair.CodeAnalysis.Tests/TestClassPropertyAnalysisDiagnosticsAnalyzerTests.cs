using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class TestClassPropertyAnalysisDiagnosticsAnalyzerTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new TestClassPropertyAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task NonTestClassAllowedMutablePropertiesAsync()
    {
        const string test = @"
public sealed class NormalClass {
    public int TestValue {get; set;}

}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task TestClassProhibitedMutablePropertyAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {get; set;}

}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0036",
                                        Message = "Properties in test classes should be read-only or const",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 5) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon }, expected);
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    Test() {
        Value = 42;
    }

    public int Value {get;}

}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithLambdaAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value => 42;

}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithCompileTimeAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {get;} = 42;

}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }

    [Fact]
    public Task TestClassAllowedGetOnlyPropertyWithWxplicitBodyAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    public int Value {
            get
            {
                return 42;
            }
        }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon });
    }

    [Fact]
    public Task TestClassAllowedInitOnlyPropertyAsync()
    {
        const string test = @"
using FunFair.Test.Common;

public sealed class Test : TestBase {

    Test() {
        Value = 42;
    }

    public int Value {get; set;}

}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0036",
                                        Message = "Properties in test classes should be read-only or const",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 5) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon }, expected);
    }

    [Fact]
    public Task TestClassProhibitedMutablePropertyWithExplicitBodyAsync()
    {
        const string test = @"
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

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0036",
                                        Message = "Properties in test classes should be read-only or const",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 5) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, new[] { WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon }, expected);
    }
}