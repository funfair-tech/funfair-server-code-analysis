using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with record declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class RecordAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Records";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleRecordsShouldBeSealed,
                                                                                   category: CATEGORY,
                                                                                   title: "Records should be sealed",
                                                                                   message: "Records should be sealed");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] { Rule }.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.RecordDeclaration);
        }

        private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not RecordDeclarationSyntax recordDeclarationSyntax)
            {
                return;
            }

            if (!recordDeclarationSyntax.Modifiers.Any(IsWhiteListedRecordModifier))
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, recordDeclarationSyntax.GetLocation()));
            }
        }

        private static bool IsWhiteListedRecordModifier(SyntaxToken syntaxToken)
        {
            SyntaxKind kind = syntaxToken.Kind();

            return kind == SyntaxKind.SealedKeyword;
        }
    }
}