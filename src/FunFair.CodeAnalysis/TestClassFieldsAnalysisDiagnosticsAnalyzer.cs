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
    public sealed class TestClassFieldsAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Classes";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleTestClassesShouldNotDefineMutableFields,
                                                                                   category: CATEGORY,
                                                                                   title: "Fields in test classes should be read-only or const",
                                                                                   message: "Fields in test classes should be read-only or const");

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
            compilationStartContext.RegisterSyntaxNodeAction(action: FieldMustBeReadOnly, SyntaxKind.FieldDeclaration);
        }

        private static void FieldMustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!TestDetection.IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                return;
            }

            if (syntaxNodeAnalysisContext.Node is not FieldDeclarationSyntax fieldDeclarationSyntax)
            {
                return;
            }

            if (fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.ReadOnlyKeyword) || fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.ConstKeyword))
            {
                // its read-only or const.
                return;
            }

            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, fieldDeclarationSyntax.GetLocation()));
        }
    }
}