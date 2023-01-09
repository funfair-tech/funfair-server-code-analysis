using System.Collections.Immutable;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for #nullable directives.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class NullableDirectiveDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleDontConfigureNullableInCode,
                                                                               category: Categories.IllegalDirectives,
                                                                               title: "Don't use #nulllable directive, make the change globally for the project",
                                                                               message: "Don't use #nulllable directive, make the change globally for the project");

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
        void LookForProhibition(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not NullableDirectiveTriviaSyntax pragmaWarningDirective)
            {
                return;
            }

            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, pragmaWarningDirective: pragmaWarningDirective);
        }

        compilationStartContext.RegisterSyntaxNodeAction(action: LookForProhibition, SyntaxKind.NullableDirectiveTrivia);
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, NullableDirectiveTriviaSyntax pragmaWarningDirective)
    {
        pragmaWarningDirective.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
    }
}