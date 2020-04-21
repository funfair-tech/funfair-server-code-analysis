using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for problems with structs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class StructAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Structs";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(Rules.RuleStructsShouldBeReadOnly,
                                                                                   CATEGORY,
                                                                                   title: "Structs should be read-only",
                                                                                   message: "Structs should be read-only");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(MustBeReadOnly, SyntaxKind.StructDeclaration);
        }

        private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!(syntaxNodeAnalysisContext.Node is StructDeclarationSyntax structDeclarationSyntax))
            {
                return;
            }

            if (!structDeclarationSyntax.Modifiers.Any(IsReadOnlyKeyword))
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, structDeclarationSyntax.GetLocation()));
            }
        }

        private static bool IsReadOnlyKeyword(SyntaxToken syntaxToken)
        {
            return syntaxToken.Kind() == SyntaxKind.ReadOnlyKeyword;
        }
    }
}