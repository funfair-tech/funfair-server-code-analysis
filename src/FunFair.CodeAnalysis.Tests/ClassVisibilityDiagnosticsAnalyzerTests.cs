using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ClassVisibilityDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ClassVisibilityDiagnosticsAnalyzer>
{
    [Fact]
    public Task ClassThatHasNothingToDoWithTestsIsNotAnErrorAsync()
    {
        const string test =
            @"
public sealed class Test
{

    public void DoIt()
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.FunFairTestCommon);
    }

    [Fact]
    public Task PublicMockBaseIsAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common.Mocks;

public sealed class Test : MockBase<string>
{
    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0029",
            message: "MockBase<T> instances must be internal",
            severity: DiagnosticSeverity.Error,
            line: 4,
            column: 1
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.FunFairTestCommon,
            expected: expected
        );
    }

    [Fact]
    public Task InternalMockBaseNotAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common.Mocks;

internal sealed class Test : MockBase<string>
{

    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.FunFairTestCommon);
    }

    [Fact]
    public Task AbstractMockBaseIsAnErrorAsync()
    {
        const string test =
            @"
using FunFair.Test.Common.Mocks;

internal abstract class Test : MockBase<string>
{

    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0030",
            message: "MockBase<T> instances must be sealed",
            severity: DiagnosticSeverity.Error,
            line: 4,
            column: 1
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.FunFairTestCommon,
            expected: expected
        );
    }
}
