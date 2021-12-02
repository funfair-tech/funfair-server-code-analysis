using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class NullableDirectiveDiagnosticsAnalyzerTests : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new NullableDirectiveDiagnosticsAnalyzer();
    }

    [Fact]
    public Task NullableDisableIsAnErrorAsync()
    {
        const string test = @"#nullable disable";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0022",
                                        Message = "Don't use #nulllable directive, make the change globally for the project",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 37)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task NullableRestoreIsAnErrorAsync()
    {
        const string test = @"#nullable restore";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0022",
                                        Message = "Don't use #nulllable directive, make the change globally for the project",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 37)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task NullableEnableIsAnErrorAsync()
    {
        const string test = @"#nullable enable";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0022",
                                        Message = "Don't use #nulllable directive, make the change globally for the project",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 37)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }
}