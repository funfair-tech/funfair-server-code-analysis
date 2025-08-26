using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
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

    [Fact]
    public Task StringComparisonInvariantCultureAsParameterIsAnErrorAsync()
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
        DoIt(StringComparison.InvariantCulture);
    }
}"

            ;

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger, expected: expected);
    }

    [Fact]
    public Task StringComparisonInvariantCultureIgnoreCaseAsParameterIsAnErrorAsync()
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
        DoIt(StringComparison.InvariantCultureIgnoreCase);
    }
}"

            ;

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger, expected: expected);
    }

    [Fact]
    public Task StringComparisonCurrentCultureAsParameterIsAnErrorAsync()
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
        DoIt(StringComparison.CurrentCulture);
    }
}"

            ;

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger, expected: expected);
    }

    [Fact]
    public Task StringComparisonCurrentCultureIgnoreCaseAsParameterIsAnErrorAsync()
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
        DoIt(StringComparison.CurrentCultureIgnoreCase);
    }
}"

            ;

        DiagnosticResult expected = Result(
            id: "FFS0020",
            message: "Parameter 'logger' must be parameter 2",
            severity: DiagnosticSeverity.Error,
            line: 7,
            column: 30
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.GenericLogger, expected: expected);
    }
}