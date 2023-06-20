using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text;
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

    #region Formatting Diagnostics

    private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
    {
        StringBuilder builder = new();

        foreach (Diagnostic diagnostic in diagnostics)
        {
            builder = builder.Append("// ")
                             .Append(diagnostic)
                             .AppendLine();

            Type analyzerType = analyzer.GetType();
            ImmutableArray<DiagnosticDescriptor> rules = analyzer.SupportedDiagnostics;

            DiagnosticDescriptor? rule = rules.FirstOrDefault(rule => rule.Id == diagnostic.Id);

            if (rule is null)
            {
                continue;
            }

            Location location = diagnostic.Location;

            if (location == Location.None)
            {
                builder = builder.Append(provider: CultureInfo.InvariantCulture, $"GetGlobalResult({analyzerType.Name}.{rule.Id})");
            }
            else
            {
                Assert.True(condition: location.IsInSource, $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostic}\r\n");

                string resultMethodName = GetResultMethodName(diagnostic);
                LinePosition linePosition = diagnostic.Location.GetLineSpan()
                                                      .StartLinePosition;

                builder = builder.Append(provider: CultureInfo.InvariantCulture,
                                         $"{resultMethodName}({linePosition.Line + 1}, {linePosition.Character + 1}, {analyzerType.Name}.{rule.Id})");
            }

            builder = builder.Append(value: ',')
                             .AppendLine();
        }

        return builder.ToString()
                      .TrimEnd()
                      .TrimEnd(',') + Environment.NewLine;
    }

    private static string GetResultMethodName(Diagnostic diagnostic)
    {
        return diagnostic.Location.SourceTree!.FilePath.EndsWith(value: ".cs", comparisonType: StringComparison.OrdinalIgnoreCase)
            ? "GetCSharpResultAt"
            : "GetBasicResultAt";
    }

    #endregion

    #region Verifier wrappers

    protected Task VerifyCSharpDiagnosticAsync(string source, params DiagnosticResult[] expected)
    {
        return this.VerifyCSharpDiagnosticAsync(source: source, Array.Empty<MetadataReference>(), expected: expected);
    }

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    protected Task VerifyCSharpDiagnosticAsync(string source, MetadataReference[] references, params DiagnosticResult[] expected)
    {
        DiagnosticAnalyzer diagnostic = AssertReallyNotNull(this.GetCSharpDiagnosticAnalyzer());

        return VerifyDiagnosticsAsync(new[]
                                      {
                                          source
                                      },
                                      references: references,
                                      language: LanguageNames.CSharp,
                                      analyzer: diagnostic,
                                      expected: expected);
    }

    [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "May be needed in future")]
    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0109: Add an overload with a Span or Memory parameter", Justification = "Won't work here")]
    protected Task VerifyCSharpDiagnosticAsync(string[] sources, params DiagnosticResult[] expected)
    {
        return this.VerifyCSharpDiagnosticAsync(sources: sources, Array.Empty<MetadataReference>(), expected: expected);
    }

    private Task VerifyCSharpDiagnosticAsync(string[] sources, MetadataReference[] references, params DiagnosticResult[] expected)
    {
        DiagnosticAnalyzer diagnostic = AssertReallyNotNull(this.GetCSharpDiagnosticAnalyzer());

        return VerifyDiagnosticsAsync(sources: sources, references: references, language: LanguageNames.CSharp, analyzer: diagnostic, expected: expected);
    }

    private static async Task VerifyDiagnosticsAsync(string[] sources,
                                                     MetadataReference[] references,
                                                     string language,
                                                     DiagnosticAnalyzer analyzer,
                                                     params DiagnosticResult[] expected)
    {
        IReadOnlyList<Diagnostic> diagnostics = await GetSortedDiagnosticsAsync(sources: sources, references: references, language: language, analyzer: analyzer);

        VerifyDiagnosticResults(actualResults: diagnostics, analyzer: analyzer, expectedResults: expected);
    }

    #endregion

    #region Actual comparisons and verifications

    private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
    {
        Diagnostic[] results = actualResults.ToArray();
        int expectedCount = expectedResults.Length;
        int actualCount = results.Length;

        if (expectedCount != actualCount)
        {
            string diagnosticsOutput = results.Length != 0
                ? FormatDiagnostics(analyzer: analyzer, results.ToArray())
                : "    NONE.";

            Assert.True(condition: false,
                        $"Mismatch between number of diagnostics returned, expected \"{expectedCount}\" actual \"{actualCount}\"\r\n\r\nDiagnostics:\r\n{diagnosticsOutput}\r\n");
        }

        for (int i = 0; i < expectedResults.Length; i++)
        {
            Diagnostic actual = results[i];
            DiagnosticResult expected = expectedResults[i];

            VerifyOneResult(analyzer: analyzer, expected: expected, actual: actual);
        }
    }

    private static void VerifyOneResult(DiagnosticAnalyzer analyzer, in DiagnosticResult expected, Diagnostic actual)
    {
        if (expected.Line == -1 && expected.Column == -1)
        {
            Assert.True(actual.Location == Location.None, $"Expected:\nA project diagnostic with No location\nActual:\n{FormatDiagnostics(analyzer: analyzer, actual)}");
        }
        else
        {
            VerifyDiagnosticLocation(analyzer: analyzer, diagnostic: actual, actual: actual.Location, expected.Locations[0]);
            VerifyAdditionalDiagnosticLocations(analyzer: analyzer, actual: actual, expected: expected);
        }

        Assert.True(actual.Id == expected.Id,
                    $"Expected diagnostic id to be \"{expected.Id}\" was \"{actual.Id}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n");

        Assert.True(actual.Severity == expected.Severity,
                    $"Expected diagnostic severity to be \"{expected.Severity.GetName()}\" was \"{actual.Severity.GetName()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n");

        Assert.True(actual.GetMessage() == expected.Message,
                    $"Expected diagnostic message to be \"{expected.Message}\" was \"{actual.GetMessage()}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n");
    }

    private static void VerifyAdditionalDiagnosticLocations(DiagnosticAnalyzer analyzer, Diagnostic actual, in DiagnosticResult expected)
    {
        Location[] additionalLocations = actual.AdditionalLocations.ToArray();

        if (additionalLocations.Length != expected.Locations.Length - 1)
        {
            Assert.True(condition: false,
                        $"Expected {expected.Locations.Length - 1} additional locations but got {additionalLocations.Length} for Diagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, actual)}\r\n");
        }

        for (int j = 0; j < additionalLocations.Length; ++j)
        {
            VerifyDiagnosticLocation(analyzer: analyzer, diagnostic: actual, additionalLocations[j], expected.Locations[j + 1]);
        }
    }

    private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, in DiagnosticResultLocation expected)
    {
        FileLinePositionSpan actualSpan = actual.GetLineSpan();

        Assert.True(
            actualSpan.Path == expected.Path || actualSpan.Path.Contains(value: "Test0.", comparisonType: StringComparison.Ordinal) &&
            expected.Path.Contains(value: "Test.", comparisonType: StringComparison.Ordinal),
            $"Expected diagnostic to be in file \"{expected.Path}\" was actually in file \"{actualSpan.Path}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n");

        LinePosition actualLinePosition = actualSpan.StartLinePosition;

        // Only check line position if there is an actual line in the real diagnostic
        if (actualLinePosition.Line > 0)
        {
            if (actualLinePosition.Line + 1 != expected.Line)
            {
                Assert.True(condition: false,
                            $"Expected diagnostic to be on line \"{expected.Line}\" was actually on line \"{actualLinePosition.Line + 10}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n");
            }
        }

        // Only check column position if there is an actual column position in the real diagnostic
        if (actualLinePosition.Character > 0)
        {
            if (actualLinePosition.Character + 1 != expected.Column)
            {
                Assert.True(condition: false,
                            $"Expected diagnostic to start at column \"{expected.Column}\" was actually at column \"{actualLinePosition.Character + 1}\"\r\n\r\nDiagnostic:\r\n    {FormatDiagnostics(analyzer: analyzer, diagnostic)}\r\n");
            }
        }
    }

    #endregion
}