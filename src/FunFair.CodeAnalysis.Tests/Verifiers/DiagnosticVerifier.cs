using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.Test.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using Xunit;
using Xunit.Sdk;

namespace FunFair.CodeAnalysis.Tests.Verifiers
{
    /// <summary>
    ///     Superclass of all Unit Tests for DiagnosticAnalyzers
    /// </summary>
    public abstract partial class DiagnosticVerifier : TestBase
    {
        #region Formatting Diagnostics

        /// <summary>
        ///     Helper method to format a Diagnostic into an easily readable string
        /// </summary>
        /// <param name="analyzer">The analyzer that this verifier tests</param>
        /// <param name="diagnostics">The Diagnostics to be formatted</param>
        /// <returns>The Diagnostics formatted as a string</returns>
        private static string FormatDiagnostics(DiagnosticAnalyzer analyzer, params Diagnostic[] diagnostics)
        {
            StringBuilder builder = new();

            for (int i = 0; i < diagnostics.Length; ++i)
            {
                builder.AppendLine("// " + diagnostics[i]);

                Type analyzerType = analyzer.GetType();
                ImmutableArray<DiagnosticDescriptor> rules = analyzer.SupportedDiagnostics;

                foreach (DiagnosticDescriptor rule in rules)
                {
                    if (rule.Id != diagnostics[i]
                        .Id)
                    {
                        continue;
                    }

                    Location location = diagnostics[i]
                        .Location;

                    if (location == Location.None)
                    {
                        builder.AppendFormat(format: "GetGlobalResult({0}.{1})", arg0: analyzerType.Name, arg1: rule.Id);
                    }
                    else
                    {
                        Assert.True(condition: location.IsInSource,
                                    $"Test base does not currently handle diagnostics in metadata locations. Diagnostic in metadata: {diagnostics[i]}\r\n");

                        string resultMethodName = diagnostics[i]
                                                  .Location.SourceTree!.FilePath.EndsWith(value: ".cs", comparisonType: StringComparison.OrdinalIgnoreCase)
                            ? "GetCSharpResultAt"
                            : "GetBasicResultAt";
                        LinePosition linePosition = diagnostics[i]
                                                    .Location.GetLineSpan()
                                                    .StartLinePosition;

                        builder.AppendFormat(format: "{0}({1}, {2}, {3}.{4})", resultMethodName, linePosition.Line + 1, linePosition.Character + 1, analyzerType.Name, rule.Id);
                    }

                    if (i != diagnostics.Length - 1)
                    {
                        builder.Append(value: ',');
                    }

                    builder.AppendLine();

                    break;
                }
            }

            return builder.ToString();
        }

        #endregion

        #region To be implemented by Test classes

        /// <summary>
        ///     Get the CSharp analyzer being tested - to be implemented in non-abstract class
        /// </summary>
        protected virtual DiagnosticAnalyzer? GetCSharpDiagnosticAnalyzer()
        {
            return null;
        }

        #endregion

        #region Verifier wrappers

        /// <summary>
        ///     Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        ///     Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
        protected Task VerifyCSharpDiagnosticAsync(string source, params DiagnosticResult[] expected)
        {
            return this.VerifyCSharpDiagnosticAsync(source: source, Array.Empty<MetadataReference>(), expected: expected);
        }

        /// <summary>
        ///     Called to test a C# DiagnosticAnalyzer when applied on the single inputted string as a source
        ///     Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="source">A class in the form of a string to run the analyzer on</param>
        /// <param name="references">The project/assemblies that are referenced by the code.</param>
        /// <param name="expected"> DiagnosticResults that should appear after the analyzer is run on the source</param>
        protected Task VerifyCSharpDiagnosticAsync(string source, MetadataReference[] references, params DiagnosticResult[] expected)
        {
            DiagnosticAnalyzer? diagnostic = this.GetCSharpDiagnosticAnalyzer();

            if (diagnostic == null)
            {
                throw new NotNullException();
            }

            return VerifyDiagnosticsAsync(new[] {source}, references: references, language: LanguageNames.CSharp, analyzer: diagnostic, expected: expected);
        }

        /// <summary>
        ///     Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        ///     Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        [SuppressMessage(category: "ReSharper", checkId: "UnusedMember.Global", Justification = "TODO: Review")]
        protected Task VerifyCSharpDiagnosticAsync(string[] sources, params DiagnosticResult[] expected)
        {
            return this.VerifyCSharpDiagnosticAsync(sources: sources, Array.Empty<MetadataReference>(), expected: expected);
        }

        /// <summary>
        ///     Called to test a C# DiagnosticAnalyzer when applied on the inputted strings as a source
        ///     Note: input a DiagnosticResult for each Diagnostic expected
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="references">The project/assemblies that are referenced by the code.</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        protected Task VerifyCSharpDiagnosticAsync(string[] sources, MetadataReference[] references, params DiagnosticResult[] expected)
        {
            DiagnosticAnalyzer? diagnostic = this.GetCSharpDiagnosticAnalyzer();

            if (diagnostic == null)
            {
                throw new NotNullException();
            }

            return VerifyDiagnosticsAsync(sources: sources, references: references, language: LanguageNames.CSharp, analyzer: diagnostic, expected: expected);
        }

        /// <summary>
        ///     General method that gets a collection of actual diagnostics found in the source after the analyzer is run,
        ///     then verifies each of them.
        /// </summary>
        /// <param name="sources">An array of strings to create source documents from to run the analyzers on</param>
        /// <param name="references">The project/assemblies that are referenced by the code.</param>
        /// <param name="language">The language of the classes represented by the source strings</param>
        /// <param name="analyzer">The analyzer to be run on the source code</param>
        /// <param name="expected">DiagnosticResults that should appear after the analyzer is run on the sources</param>
        private static async Task VerifyDiagnosticsAsync(string[] sources,
                                                         MetadataReference[] references,
                                                         string language,
                                                         DiagnosticAnalyzer analyzer,
                                                         params DiagnosticResult[] expected)
        {
            Diagnostic[] diagnostics = await GetSortedDiagnosticsAsync(sources: sources, references: references, language: language, analyzer: analyzer);

            VerifyDiagnosticResults(actualResults: diagnostics, analyzer: analyzer, expectedResults: expected);
        }

        #endregion

        #region Actual comparisons and verifications

        /// <summary>
        ///     Checks each of the actual Diagnostics found and compares them with the corresponding DiagnosticResult in the array of expected results.
        ///     Diagnostics are considered equal only if the DiagnosticResultLocation, Id, Severity, and Message of the DiagnosticResult match the actual diagnostic.
        /// </summary>
        /// <param name="actualResults">The Diagnostics found by the compiler after running the analyzer on the source code</param>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="expectedResults">Diagnostic Results that should have appeared in the code</param>
        private static void VerifyDiagnosticResults(IEnumerable<Diagnostic> actualResults, DiagnosticAnalyzer analyzer, params DiagnosticResult[] expectedResults)
        {
            Diagnostic[] results = actualResults.ToArray();
            int expectedCount = expectedResults.Length;
            int actualCount = results.Length;

            if (expectedCount != actualCount)
            {
                string diagnosticsOutput = results.Any() ? FormatDiagnostics(analyzer: analyzer, results.ToArray()) : "    NONE.";

                Assert.True(condition: false,
                            string.Format(format: "Mismatch between number of diagnostics returned, expected \"{0}\" actual \"{1}\"\r\n\r\nDiagnostics:\r\n{2}\r\n",
                                          arg0: expectedCount,
                                          arg1: actualCount,
                                          arg2: diagnosticsOutput));
            }

            for (int i = 0; i < expectedResults.Length; i++)
            {
                Diagnostic actual = results.ElementAt(i);
                DiagnosticResult expected = expectedResults[i];

                if (expected.Line == -1 && expected.Column == -1)
                {
                    if (actual.Location != Location.None)
                    {
                        Assert.True(condition: false,
                                    string.Format(format: "Expected:\nA project diagnostic with No location\nActual:\n{0}", FormatDiagnostics(analyzer: analyzer, actual)));
                    }
                }
                else
                {
                    VerifyDiagnosticLocation(analyzer: analyzer, diagnostic: actual, actual: actual.Location, expected.Locations.First());
                    Location[] additionalLocations = actual.AdditionalLocations.ToArray();

                    if (additionalLocations.Length != expected.Locations.Length - 1)
                    {
                        Assert.True(condition: false,
                                    string.Format(format: "Expected {0} additional locations but got {1} for Diagnostic:\r\n    {2}\r\n",
                                                  expected.Locations.Length - 1,
                                                  arg1: additionalLocations.Length,
                                                  FormatDiagnostics(analyzer: analyzer, actual)));
                    }

                    for (int j = 0; j < additionalLocations.Length; ++j)
                    {
                        VerifyDiagnosticLocation(analyzer: analyzer, diagnostic: actual, additionalLocations[j], expected.Locations[j + 1]);
                    }
                }

                if (actual.Id != expected.Id)
                {
                    Assert.True(condition: false,
                                string.Format(format: "Expected diagnostic id to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                              arg0: expected.Id,
                                              arg1: actual.Id,
                                              FormatDiagnostics(analyzer: analyzer, actual)));
                }

                if (actual.Severity != expected.Severity)
                {
                    Assert.True(condition: false,
                                string.Format(format: "Expected diagnostic severity to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                              arg0: expected.Severity,
                                              arg1: actual.Severity,
                                              FormatDiagnostics(analyzer: analyzer, actual)));
                }

                if (actual.GetMessage() != expected.Message)
                {
                    Assert.True(condition: false,
                                string.Format(format: "Expected diagnostic message to be \"{0}\" was \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                              arg0: expected.Message,
                                              actual.GetMessage(),
                                              FormatDiagnostics(analyzer: analyzer, actual)));
                }
            }
        }

        /// <summary>
        ///     Helper method to VerifyDiagnosticResult that checks the location of a diagnostic and compares it with the location in the expected DiagnosticResult.
        /// </summary>
        /// <param name="analyzer">The analyzer that was being run on the sources</param>
        /// <param name="diagnostic">The diagnostic that was found in the code</param>
        /// <param name="actual">The Location of the Diagnostic found in the code</param>
        /// <param name="expected">The DiagnosticResultLocation that should have been found</param>
        private static void VerifyDiagnosticLocation(DiagnosticAnalyzer analyzer, Diagnostic diagnostic, Location actual, DiagnosticResultLocation expected)
        {
            FileLinePositionSpan actualSpan = actual.GetLineSpan();

            Assert.True(actualSpan.Path == expected.Path || actualSpan.Path.Contains(value: "Test0.") && expected.Path.Contains(value: "Test."),
                        string.Format(format: "Expected diagnostic to be in file \"{0}\" was actually in file \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                      arg0: expected.Path,
                                      arg1: actualSpan.Path,
                                      FormatDiagnostics(analyzer: analyzer, diagnostic)));

            LinePosition actualLinePosition = actualSpan.StartLinePosition;

            // Only check line position if there is an actual line in the real diagnostic
            if (actualLinePosition.Line > 0)
            {
                if (actualLinePosition.Line + 1 != expected.Line)
                {
                    Assert.True(condition: false,
                                string.Format(format: "Expected diagnostic to be on line \"{0}\" was actually on line \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                              arg0: expected.Line,
                                              actualLinePosition.Line + 1,
                                              FormatDiagnostics(analyzer: analyzer, diagnostic)));
                }
            }

            // Only check column position if there is an actual column position in the real diagnostic
            if (actualLinePosition.Character > 0)
            {
                if (actualLinePosition.Character + 1 != expected.Column)
                {
                    Assert.True(condition: false,
                                string.Format(format: "Expected diagnostic to start at column \"{0}\" was actually at column \"{1}\"\r\n\r\nDiagnostic:\r\n    {2}\r\n",
                                              arg0: expected.Column,
                                              actualLinePosition.Character + 1,
                                              FormatDiagnostics(analyzer: analyzer, diagnostic)));
                }
            }
        }

        #endregion
    }
}