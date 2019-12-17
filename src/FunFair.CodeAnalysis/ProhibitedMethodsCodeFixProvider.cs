using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Text;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    /// Prohibited methods code fix provider.
    /// </summary>
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ProhibitedMethodsCodeFixProvider))]
    [Shared]
    public class ProhibitedMethodsCodeFixProvider : CodeFixProvider
    {
        private const string TITLE = "Call DateTime.UtcNow rather than DateTime.Now";

        /// <inheritdoc />
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ProhibitedMethodsDiagnosticsAnalyzer.DiagnosticId);

        /// <inheritdoc />
        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        /// <inheritdoc />
        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            Diagnostic diagnostic = context.Diagnostics.First();
            TextSpan diagnosticSpan = diagnostic.Location.SourceSpan;

            return Task.Run(action: () => context.RegisterCodeFix(
                                        CodeAction.Create(TITLE, createChangedDocument: c => this.ReplaceWithUtcNowAsync(context.Document, diagnosticSpan), TITLE),
                                        diagnostic));
        }

        private async Task<Document> ReplaceWithUtcNowAsync(Document document, TextSpan span)
        {
            SourceText text = await document.GetTextAsync();
            string repl = @"DateTime.UtcNow";

            if (Regex.Replace(text.GetSubText(span)
                                  .ToString(),
                              pattern: @"\s+",
                              string.Empty) == "System.DateTime.Now")
            {
                repl = "System.DateTime.UtcNow";
            }

            SourceText newtext = text.Replace(span, repl);

            return document.WithText(newtext);
        }
    }
}