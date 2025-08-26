using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedEnumMembersAnalyzerTests : DiagnosticAnalyzerVerifier<ProhibitedEnumMembersAnalyzer>
{
    [Fact]
    public Task StringComparisonOrdinalAsParameterIsNotAnErrorAsync()
    {
        const string test =
                @"
using System;

public sealed class Test {

    public bool DoIt(StringComparison comparison)
    {
        return comparison == StringComparison.Ordinal ||
               comparison == System.StringComparison.OrdinalIgnoreCase;
    }

    public void Check()
    {
        DoIt(StringComparison.Ordinal);
    }
}"

            ;

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }

    [Fact]
    public Task StringComparisonOrdinalIgnoreCaseAsParameterIsNotAnErrorAsync()
    {
        const string test =
                @"
using System;

public sealed class Test {

    public bool DoIt(StringComparison comparison)
    {
        return comparison == StringComparison.Ordinal ||
               comparison == System.StringComparison.OrdinalIgnoreCase;
    }

    public void Check()
    {
        DoIt(StringComparison.OrdinalIgnoreCase);
    }
}"

            ;

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger);
    }
}