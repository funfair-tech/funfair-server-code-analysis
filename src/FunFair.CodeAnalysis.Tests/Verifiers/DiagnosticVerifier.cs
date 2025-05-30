using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;

namespace FunFair.CodeAnalysis.Tests.Verifiers;

public abstract partial class DiagnosticVerifier : TestBase
{
    #region To be implemented by Test classes

    protected virtual DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer()
    {
        return null;
    }

    #endregion

    protected static DiagnosticResult Result(
        string id,
        string message,
        DiagnosticSeverity severity,
        int line,
        int column
    )
    {
        DiagnosticResultLocation location = new(path: "Test0.cs", line: line, column: column);

        return new([location], Severity: severity, Id: id, Message: message);
    }

    #region Formatting Diagnostics

    private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
    {
        StringBuilder builder = new();

        foreach (Diagnostic diagnostic in diagnostics)
        {
            builder = builder.Append("// ").Append(diagnostic).AppendLine();

            Type analyzerType = analyzer.GetType();
            ImmutableArray<DiagnosticDescriptor> rules = analyzer.SupportedDiagnostics;

            DiagnosticDescriptor? rule = rules.FirstOrDefault(rule =>
                StringComparer.Ordinal.Equals(x: rule.Id, y: diagnostic.Id)
            );

            if (rule is null)
            {
                continue;
            }

            Location location = diagnostic.Location;

            if (location == Location.None)
            {
                builder = builder.Append(
                    provider: CultureInfo.InvariantCulture,
                    $"GetGlobalResult({analyzerType.Name}.{rule.Id})"
                );
            }
            else
            {
                Assert.True(
                    condition: location.IsInSource,
                    $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostic}\r\n"
                );

                string resultMethodName = GetResultMethodName(diagnostic);
                LinePosition linePosition = GetStartLinePosition(diagnostic);

                builder = builder.Append(
                    provider: CultureInfo.InvariantCulture,
                    $"{resultMethodName}({linePosition.Line + 1}, {linePosition.Character + 1}, {analyzerType.Name}.{rule.Id})"
                );
            }

            builder = builder.Append(value: ',').AppendLine();
        }

        return builder.ToString().TrimEnd().TrimEnd(',') + Environment.NewLine;
    }

    private static LinePosition GetStartLinePosition(Diagnostic diagnostic)
    {
        return diagnostic.Location.GetLineSpan().StartLinePosition;
    }

    private static string GetResultMethodName(Diagnostic diagnostic)
    {
        SyntaxTree sourceTree =
            diagnostic.Location.SourceTree
            ?? throw new InvalidOperationException(message: "Diagnostic has no source location");

        return sourceTree.FilePath.EndsWith(value: ".cs", comparisonType: StringComparison.OrdinalIgnoreCase)
            ? "GetCSharpResultAt"
            : "GetBasicResultAt";
    }

    #endregion

    #region Verifier wrappers

    protected Task VerifyCSharpDiagnosticAsync(string source, MetadataReference reference, in DiagnosticResult expected)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, [reference], [expected]);
    }

    protected Task VerifyCSharpDiagnosticAsync(string source)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, [], []);
    }

    protected Task VerifyCSharpDiagnosticAsync(string source, MetadataReference reference)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, [reference], []);
    }

    protected Task VerifyCSharpDiagnosticAsync(string source, in DiagnosticResult expected)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, [], [expected]);
    }

    protected Task VerifyCSharpDiagnosticAsync(string source, IReadOnlyList<DiagnosticResult> expected)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, [], expected: expected);
    }

    protected Task VerifyCSharpDiagnosticAsync(string source, IReadOnlyList<MetadataReference> references)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, references: references, []);
    }

    protected Task VerifyCSharpDiagnosticAsync(
        string source,
        MetadataReference references,
        IReadOnlyList<DiagnosticResult> expected
    )
    {
        IReadOnlyList<MetadataReference> refs = [references];

        return this.VerifyCSharpDiagnosticAsync(source: source, references: refs, expected: expected);
    }

    protected Task VerifyCSharpDiagnosticAsync(
        string source,
        IReadOnlyList<MetadataReference> references,
        in DiagnosticResult expected
    )
    {
        IReadOnlyList<DiagnosticResult> exp = [expected];

        return this.VerifyCSharpDiagnosticAsync(source: source, references: references, expected: exp);
    }

    protected Task VerifyCSharpDiagnosticAsync(
        string source,
        IReadOnlyList<MetadataReference> references,
        IReadOnlyList<DiagnosticResult> expected
    )
    {
        CancellationToken cancellationToken = this.CancellationToken();
        DiagnosticAnalyzer diagnostic = AssertReallyNotNull(this.GetCSharpDiagnosticAnalyzer());

        return VerifyDiagnosticsAsync(
            [source],
            references: references,
            language: LanguageNames.CSharp,
            analyzer: diagnostic,
            expected: expected,
            cancellationToken: cancellationToken
        );
    }

    private static async Task VerifyDiagnosticsAsync(
        IReadOnlyList<string> sources,
        IReadOnlyList<MetadataReference> references,
        string language,
        DiagnosticAnalyzer analyzer,
        IReadOnlyList<DiagnosticResult> expected,
        CancellationToken cancellationToken
    )
    {
        IReadOnlyList<Diagnostic> diagnostics = await GetSortedDiagnosticsAsync(
            sources: sources,
            references: references,
            language: language,
            analyzer: analyzer,
            cancellationToken: cancellationToken
        );

        VerifyDiagnosticResults(actualResults: diagnostics, analyzer: analyzer, expectedResults: expected);
    }

    #endregion

    #region Actual comparisons and verifications

    private static void VerifyDiagnosticResults(
        IReadOnlyList<Diagnostic> actualResults,
        DiagnosticAnalyzer analyzer,
        IReadOnlyList<DiagnosticResult> expectedResults
    )
    {
        int expectedCount = expectedResults.Count;
        int actualCount = actualResults.Count;

        if (expectedCount != actualCount)
        {
            string diagnosticsOutput =
                actualResults.Count != 0 ? FormatDiagnostics(analyzer: analyzer, [.. actualResults]) : "    NONE.";

            Assert.Fail(
                $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n"
            );
        }

        for (int i = 0; i < expectedResults.Count; i++)
        {
            VerifyOneResult(analyzer: analyzer, expectedResults[i], actualResults[i]);
        }
    }

    private static void VerifyOneResult(DiagnosticAnalyzer analyzer, in DiagnosticResult expected, Diagnostic actual)
    {
        if (IsInvalidLocation(expected))
        {
            Assert.True(
                actual.Location == Location.None,
                $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzer: analyzer, actual)}"
            );
        }
        else
        {
            VerifyDiagnosticLocation(
                analyzer: analyzer,
                diagnostic: actual,
                actual: actual.Location,
                expected.Locations[0]
            );
            VerifyAdditionalDiagnosticLocations(analyzer: analyzer, actual: actual, expected: expected);
        }

        Assert.True(
            StringComparer.Ordinal.Equals(x: actual.Id, y: expected.Id),
            $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n"
        );

        Assert.True(
            actual.Severity == expected.Severity,
            $"Expected diagnostic severity to be \"{expected.Severity.GetName()}\" was \"{actual.Severity.GetName()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n"
        );

        Assert.True(
            StringComparer.Ordinal.Equals(actual.GetMessage(), y: expected.Message),
            $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n"
        );
    }

    private static bool IsInvalidLocation(in DiagnosticResult expected)
    {
        return expected is { Line: -1, Column: -1 };
    }

    private static void VerifyAdditionalDiagnosticLocations(
        DiagnosticAnalyzer analyzer,
        Diagnostic actual,
        in DiagnosticResult expected
    )
    {
        IReadOnlyList<Location> additionalLocations = [.. actual.AdditionalLocations];

        if (additionalLocations.Count != expected.Locations.Count - 1)
        {
            Assert.Fail(
                $"Expected {expected.Locations.Count - 1} additional locations but got {additionalLocations.Count} for Diagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n"
            );
        }

        for (int j = 0; j < additionalLocations.Count; ++j)
        {
            VerifyDiagnosticLocation(
                analyzer: analyzer,
                diagnostic: actual,
                additionalLocations[j],
                expected.Locations[j + 1]
            );
        }
    }

    private static void VerifyDiagnosticLocation(
        DiagnosticAnalyzer analyzer,
        Diagnostic diagnostic,
        Location actual,
        in DiagnosticResultLocation expected
    )
    {
        FileLinePositionSpan actualSpan = actual.GetLineSpan();

        Assert.True(
            StringComparer.Ordinal.Equals(x: actualSpan.Path, y: expected.Path)
                || actualSpan.Path.Contains(value: "Test0.", comparisonType: StringComparison.Ordinal)
                    && expected.Path.Contains(value: "Test.", comparisonType: StringComparison.Ordinal),
            $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n"
        );

        LinePosition actualLinePosition = actualSpan.StartLinePosition;

        // Only check line position if there is an actual line in the real diagnostic
        if (actualLinePosition.Line > 0)
        {
            if (actualLinePosition.Line + 1 != expected.Line)
            {
                Assert.Fail(
                    $"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualLinePosition.Line + 10}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n"
                );
            }
        }

        // Only check column position if there is an actual column position in the real diagnostic
        if (actualLinePosition.Character > 0)
        {
            if (actualLinePosition.Character + 1 != expected.Column)
            {
                Assert.Fail(
                    $"Expected diagnostic to start at column \"{expected.Column}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n"
                );
            }
        }
    }

    #endregion
}
