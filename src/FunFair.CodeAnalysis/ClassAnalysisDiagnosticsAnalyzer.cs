using System.Collections.Immutable;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClassAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleClassesShouldBeStaticSealedOrAbstract,
        category: Categories.Classes,
        title: "Classes should be static, sealed or abstract",
        message: "Classes should be static, sealed or abstract"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

    private static readonly ImmutableHashSet<SyntaxKind> WhitelistedModifiers = ImmutableHashSet.Create(
        SyntaxKind.StaticKeyword,
        SyntaxKind.AbstractKeyword,
        SyntaxKind.SealedKeyword
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: CheckClassModifiers, SyntaxKind.ClassDeclaration);
    }

    private static void CheckClassModifiers(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        bool hasWhitelistedModifier = classDeclarationSyntax.Modifiers.Any(IsWhiteListedClassModifier);
        bool hasBenchmarkMethods = BenchmarkDetection.ClassHasBenchmarkMethods(classDeclarationSyntax);

        // Regular classes must be sealed/static/abstract.
        // Benchmark classes must NOT be sealed/static/abstract (BenchmarkDotNet requires inheritable classes).
        // Both conditions being the same is an error.
        if (hasBenchmarkMethods == hasWhitelistedModifier)
        {
            classDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
        }
    }

    private static bool IsWhiteListedClassModifier(SyntaxToken syntaxToken)
    {
        SyntaxKind kind = syntaxToken.Kind();

        return WhitelistedModifiers.Contains(kind);
    }
}
