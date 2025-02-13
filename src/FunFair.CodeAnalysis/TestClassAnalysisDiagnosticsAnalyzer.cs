using System.Collections.Immutable;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class TestClassAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleTestClassesShouldBeStaticSealedOrAbstractDerivedFromTestBase,
        category: Categories.Classes,
        title: "Test classes should be derived from TestBase",
        message: "Test classes should be derived from TestBase"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None
        );
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(
            action: MustDeriveFromTestBase,
            SyntaxKind.MethodDeclaration
        );
    }

    private static void MustDeriveFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return;
        }

        if (
            IsTestMethodInClassNotDerivedFromTestBase(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                methodDeclarationSyntax: methodDeclarationSyntax
            )
        )
        {
            methodDeclarationSyntax.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: Rule
            );
        }
    }

    private static bool IsTestMethodInClassNotDerivedFromTestBase(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        MethodDeclarationSyntax methodDeclarationSyntax
    )
    {
        return TestDetection.IsTestMethod(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                methodDeclarationSyntax: methodDeclarationSyntax
            ) && !TestDetection.IsDerivedFromTestBase(syntaxNodeAnalysisContext);
    }
}
