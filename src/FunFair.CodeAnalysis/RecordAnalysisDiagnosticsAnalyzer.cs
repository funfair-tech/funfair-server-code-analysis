using System.Collections.Immutable;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class RecordAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleRecordsShouldBeSealed,
        category: Categories.Records,
        title: "Records should be sealed",
        message: "Records should be sealed"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: CheckRecordIsSealed, SyntaxKind.RecordDeclaration);
    }

    private static void CheckRecordIsSealed(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not RecordDeclarationSyntax recordDeclarationSyntax)
        {
            return;
        }

        if (!recordDeclarationSyntax.Modifiers.Any(SyntaxKind.SealedKeyword))
        {
            recordDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
        }
    }
}