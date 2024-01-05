using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class DebuggerDisplayAnalysisDiagnosticsAnalyzerTest : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new DebuggerDisplayAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task RecordWithDebuggerDisplayIsAnErrorAsync()
    {
        const string test = "public sealed record Test {}";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0038",
                                        Message = "Should have DebuggerDisplay attribute",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task RecordStructWithNoDebuggerDisplayIsAnErrorAsync()
    {
        const string test = "public readonly record struct Test {  }";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0038",
                                        Message = "Should have DebuggerDisplay attribute",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
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