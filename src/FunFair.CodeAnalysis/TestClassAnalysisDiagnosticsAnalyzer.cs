using System.Collections.Immutable;
using System.Linq;
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
    public sealed class TestClassAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Classes";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(Rules.RuleTestClassesShouldBeStaticSealedOrAbstractDerivedFromTestBase,
                                                                                   CATEGORY,
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
            compilationStartContext.RegisterSyntaxNodeAction(MustDeriveFromTestBase, SyntaxKind.MethodDeclaration);
        }

        private static void MustDeriveFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!(syntaxNodeAnalysisContext.Node is MethodDeclarationSyntax methodDeclarationSyntax))
            {
                return;
            }

            if (!IsTestMethod(syntaxNodeAnalysisContext, methodDeclarationSyntax))
            {
                return;
            }

            if (!IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, methodDeclarationSyntax.Parent!.GetLocation()));
            }
        }

        private static bool IsDerivedFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            var cl = syntaxNodeAnalysisContext.ContainingSymbol;

            for (INamedTypeSymbol? parent = cl.ContainingType; parent != null; parent = parent.BaseType)
            {
                if (parent.ToString() == "FunFair.Test.Common.TestBase")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTestMethod(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.AttributeLists.SelectMany(collectionSelector: attributeListSyntax => attributeListSyntax.Attributes,
                                                                     resultSelector: (attributeListSyntax, attribute) =>
                                                                                         ModelExtensions.GetSymbolInfo(syntaxNodeAnalysisContext.SemanticModel, attribute))
                                          .SelectMany(collectionSelector: symbolInfo => symbolInfo.CandidateSymbols,
                                                      resultSelector: (symbolInfo, candidate) => candidate.ContainingType.ToString())
                                          .Any(predicate: attributeType => IsTestMethodAttribute(attributeType));
        }

        private static bool IsTestMethodAttribute(string attributeType)
        {
            return attributeType == @"Xunit.FactAttribute" || attributeType == @"Xunit.TheoryAttribute";
        }
    }
}