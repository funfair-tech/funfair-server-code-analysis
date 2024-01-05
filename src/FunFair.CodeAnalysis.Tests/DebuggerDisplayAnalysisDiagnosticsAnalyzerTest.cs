using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class DebuggerDisplayAnalysisDiagnosticsAnalyzerTest : DiagnosticAnalyzerVerifier<DebuggerDisplayAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task RecordWithDebuggerDisplayIsAnErrorAsync()
    {
        const string test = "public sealed record Test {}";
        DiagnosticResult expected = Result(id: "FFS0038", message: "Should have DebuggerDisplay attribute", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task RecordStructWithNoDebuggerDisplayIsAnErrorAsync()
    {
        const string test = "public readonly record struct Test {  }";
        DiagnosticResult expected = Result(id: "FFS0038", message: "Should have DebuggerDisplay attribute", severity: DiagnosticSeverity.Error, line: 12, column: 25);

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task RecordWithDebuggerDisplayNotAnErrorAsync()
    {
        const string test = @"using System.Diagnostics;

[DebuggerDisplay(""Value : {Value}"")]
public sealed record Test {
    public int Value { get; init; }
}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RecordStructWithDebuggerDisplayNotAnErrorAsync()
    {
        const string test = @"using System.Diagnostics;

[DebuggerDisplay(""Value : {Value}"")]
public readonly record struct Test {
    public int Value { get; init; }
}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RecordWithFullyQualifiedDebuggerDisplayNotAnErrorAsync()
    {
        const string test = @"[System.Diagnostics.DebuggerDisplay(""Value : {Value}"")]
public sealed record Test {
    public int Value { get; init; }
}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RecordStructWithFullyQualifiedDebuggerDisplayNotAnErrorAsync()
    {
        const string test = @"[System.Diagnostics.DebuggerDisplay(""Value : {Value}"")]
public readonly record struct Test {
    public int Value { get; init; }
}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}