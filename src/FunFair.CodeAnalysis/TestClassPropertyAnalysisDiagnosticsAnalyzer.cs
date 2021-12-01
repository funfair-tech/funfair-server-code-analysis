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
    ///     Looks for issues with class declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TestClassPropertyAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleTestClassesShouldNotDefineMutableProperties,
                                                                                   category: Categories.Classes,
                                                                                   title: "Properties in test classes should be read-only or const",
                                                                                   message: "Properties in test classes should be read-only or const");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            new[]
            {
                Rule
            }.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: PropertyMustBeReadOnly, SyntaxKind.PropertyDeclaration);
        }

        private static void PropertyMustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!TestDetection.IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                return;
            }

            if (syntaxNodeAnalysisContext.Node is not PropertyDeclarationSyntax propertyDeclarationSyntax)
            {
                return;
            }

            if (propertyDeclarationSyntax.AccessorList?.Accessors.Any(IsMutable) == true)
            {
                // its read-only or const.
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, propertyDeclarationSyntax.GetLocation()));
            }
        }

        private static bool IsMutable(AccessorDeclarationSyntax a)
        {
            return a.Kind() == SyntaxKind.SetAccessorDeclaration;
        }
    }
}