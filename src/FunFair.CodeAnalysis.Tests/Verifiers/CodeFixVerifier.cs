using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
using Xunit;
using Xunit.Sdk;

namespace FunFair.CodeAnalysis.Tests.Verifiers
{
    /// <summary>
    ///     Superclass of all Unit tests made for diagnostics with codefixes.
    ///     Contains methods used to verify correctness of codefixes
    /// </summary>
    public abstract partial class CodeFixVerifier : DiagnosticVerifier
    {
        /// <summary>
        ///     Returns the codefix being tested (C#) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for CSharp code</returns>
        protected virtual CodeFixProvider? GetCSharpCodeFixProvider()
        {
            return null;
        }

        /// <summary>
        ///     Returns the codefix being tested (VB) - to be implemented in non-abstract class
        /// </summary>
        /// <returns>The CodeFixProvider to be used for VisualBasic code</returns>
        // ReSharper disable once UnusedMember.Global
        protected virtual CodeFixProvider? GetBasicCodeFixProvider()
        {
            return null;
        }

        /// <summary>
        ///     Called to test a C# codefix when applied on the inputted string as a source
        /// </summary>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        // ReSharper disable once UnusedMember.Global
        protected Task VerifyCSharpFixAsync(string oldSource, string newSource, int? codeFixIndex = null, bool allowNewCompilerDiagnostics = false)
        {
            DiagnosticAnalyzer? analyzer = this.GetCSharpDiagnosticAnalyzer();

            if (analyzer == null)
            {
                throw new NotNullException();
            }

            CodeFixProvider? codeFixProvider = this.GetCSharpCodeFixProvider();

            if (codeFixProvider == null)
            {
                throw new NotNullException();
            }

            return VerifyFixAsync(language: LanguageNames.CSharp,
                                  analyzer: analyzer,
                                  codeFixProvider: codeFixProvider,
                                  oldSource: oldSource,
                                  newSource: newSource,
                                  codeFixIndex: codeFixIndex,
                                  allowNewCompilerDiagnostics: allowNewCompilerDiagnostics);
        }

        /// <summary>
        ///     General verifier for codefixes.
        ///     Creates a Document from the source string, then gets diagnostics on it and applies the relevant codefixes.
        ///     Then gets the string after the codefix is applied and compares it with the expected result.
        ///     Note: If any codefix causes new diagnostics to show up, the test fails unless allowNewCompilerDiagnostics is set to true.
        /// </summary>
        /// <param name="language">The language the source code is in</param>
        /// <param name="analyzer">The analyzer to be applied to the source code</param>
        /// <param name="codeFixProvider">The codefix to be applied to the code wherever the relevant Diagnostic is found</param>
        /// <param name="oldSource">A class in the form of a string before the CodeFix was applied to it</param>
        /// <param name="newSource">A class in the form of a string after the CodeFix was applied to it</param>
        /// <param name="codeFixIndex">Index determining which codefix to apply if there are multiple</param>
        /// <param name="allowNewCompilerDiagnostics">A bool controlling whether or not the test will fail if the CodeFix introduces other warnings after being applied</param>
        private static async Task VerifyFixAsync(string language,
                                                 DiagnosticAnalyzer analyzer,
                                                 CodeFixProvider codeFixProvider,
                                                 string oldSource,
                                                 string newSource,
                                                 int? codeFixIndex,
                                                 bool allowNewCompilerDiagnostics)
        {
            Document document = CreateDocument(source: oldSource, language: language);
            Diagnostic[] analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer: analyzer, new[] {document});
            Diagnostic[] compilerDiagnostics = await GetCompilerDiagnosticsAsync(document);
            int attempts = analyzerDiagnostics.Length;

            for (int i = 0; i < attempts; ++i)
            {
                List<CodeAction> actions = new();
                CodeFixContext context = new(document: document, analyzerDiagnostics[0], registerCodeFix: (a, _) => actions.Add(a), cancellationToken: CancellationToken.None);
                await codeFixProvider.RegisterCodeFixesAsync(context);

                if (!actions.Any())
                {
                    break;
                }

                if (codeFixIndex != null)
                {
                    document = await ApplyFixAsync(document: document, actions.ElementAt((int) codeFixIndex));

                    break;
                }

                document = await ApplyFixAsync(document: document, actions.ElementAt(index: 0));
                analyzerDiagnostics = await GetSortedDiagnosticsFromDocumentsAsync(analyzer: analyzer, new[] {document});

                IEnumerable<Diagnostic> newCompilerDiagnostics = GetNewDiagnostics(diagnostics: compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

                //check if applying the code fix introduced any new compiler diagnostics
                if (!allowNewCompilerDiagnostics && newCompilerDiagnostics.Any())
                {
                    SyntaxNode? syntaxRoot = await document.GetSyntaxRootAsync();

                    if (syntaxRoot == null)
                    {
                        throw new NotNullException();
                    }

                    // Format and get the compiler diagnostics again so that the locations make sense in the output
                    document = document.WithSyntaxRoot(Formatter.Format(node: syntaxRoot, annotation: Formatter.Annotation, workspace: document.Project.Solution.Workspace));

                    newCompilerDiagnostics = GetNewDiagnostics(diagnostics: compilerDiagnostics, await GetCompilerDiagnosticsAsync(document));

                    SyntaxNode? sr = await document.GetSyntaxRootAsync();

                    if (sr == null)
                    {
                        throw new NotNullException();
                    }

                    Assert.True(condition: false,
                                string.Format(format: "Fix introduced new compiler diagnostics:\r\n{0}\r\n\r\nNew document:\r\n{1}\r\n",
                                              string.Join(separator: "\r\n", newCompilerDiagnostics.Select(selector: d => d.ToString())),
                                              sr.ToFullString()));
                }

                //check if there are analyzer diagnostics left after the code fix
                if (!analyzerDiagnostics.Any())
                {
                    break;
                }
            }

            //after applying all of the code fixes, compare the resulting string to the inputted one
            string actual = await GetStringFromDocumentAsync(document);
            Assert.Equal(expected: newSource, actual: actual);
        }
    }
}