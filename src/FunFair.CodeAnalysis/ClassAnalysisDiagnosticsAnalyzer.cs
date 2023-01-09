using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for issues with class declarations
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClassAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleClassesShouldBeStaticSealedOrAbstract,
                                                                               category: Categories.Classes,
                                                                               title: "Classes should be static, sealed or abstract",
                                                                               message: "Classes should be static, sealed or abstract");

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
        compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.ClassDeclaration);
    }

    private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        if (!classDeclarationSyntax.Modifiers.Any(IsWhiteListedClassModifier))
        {
            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, classDeclarationSyntax: classDeclarationSyntax);
        }
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ClassDeclarationSyntax classDeclarationSyntax)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, classDeclarationSyntax.GetLocation()));
    }

    private static bool IsWhiteListedClassModifier(SyntaxToken syntaxToken)
    {
        SyntaxKind kind = syntaxToken.Kind();

        return kind is SyntaxKind.StaticKeyword or SyntaxKind.AbstractKeyword or SyntaxKind.SealedKeyword;
    }
}