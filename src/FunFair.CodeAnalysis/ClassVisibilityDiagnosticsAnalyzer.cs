using System.Collections.Immutable;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with class declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassVisibilityDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Classes";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleTestClassesShouldBeStaticSealedOrAbstractDerivedFromTestBase,
                                                                                   category: CATEGORY,
                                                                                   title: "Test classes should be derived from TestBase",
                                                                                   message: "Test classes should be derived from TestBase");

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
            compilationStartContext.RegisterSyntaxNodeAction(action: CheckClassVisibility, SyntaxKind.ClassDeclaration);
        }

        private static void CheckClassVisibility(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!(syntaxNodeAnalysisContext.Node is ClassDeclarationSyntax classDeclarationSyntax))
            {
                return;
            }

            if (!IsDerivedFromMockBase(syntaxNodeAnalysisContext))
            {
                return;
            }

            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, classDeclarationSyntax.GetLocation()));
        }

        private static bool IsDerivedFromMockBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            ISymbol? containingType = syntaxNodeAnalysisContext.ContainingSymbol;

            if (containingType == null)
            {
                return false;
            }

            for (INamedTypeSymbol? parent = containingType.ContainingType; parent != null; parent = parent.BaseType)
            {
                if (SymbolDisplay.ToDisplayString(parent) == "FunFair.Test.Common.MockBase<>")
                {
                    return true;
                }
            }

            return false;
        }
    }
}