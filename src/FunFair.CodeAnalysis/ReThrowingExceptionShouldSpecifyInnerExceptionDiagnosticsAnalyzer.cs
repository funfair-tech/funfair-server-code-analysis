
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleMustPassInterExceptionToExceptionsThrownInCatchBlock,
        category: Categories.Exceptions,
        title: "Pass an inner exception when thrown from a catch clause",
        message: "Provide '{0}' as an inner exception when thrown from catch clauses"
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
        compilationStartContext.RegisterSyntaxNodeAction(action: CheckCatchClause, SyntaxKind.CatchClause);
    }

    private static void CheckCatchClause(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not CatchClauseSyntax catchClause)
        {
            return;
        }

        string? exceptionVariable = catchClause.Declaration?.Identifier.Text;

        if (exceptionVariable is null)
        {
            return;
        }

        IReadOnlyList<ExpressionSyntax> throwExpressions = GetAllThrowExpressions(catchClause.Block);

        if (throwExpressions.Count == 0)
        {
            return;
        }

        foreach (ExpressionSyntax expression in throwExpressions)
        {
            ArgumentListSyntax? argumentList = expression switch
            {
                ObjectCreationExpressionSyntax objectCreation => objectCreation.ArgumentList,
                InvocationExpressionSyntax invocation => invocation.ArgumentList,
                _ => null
            };

            if (argumentList is not null)
            {
                CheckArgumentList(
                    argumentList: argumentList,
                    exceptionVariable: exceptionVariable,
                    syntaxNodeContext: syntaxNodeAnalysisContext,
                    location: expression.GetLocation()
                );
            }
        }
    }

    private static void CheckArgumentList(
        ArgumentListSyntax argumentList,
        string exceptionVariable,
        in SyntaxNodeAnalysisContext syntaxNodeContext,
        Location location
    )
    {
        if (argumentList.Arguments.Count == 0)
        {
            ReportDiagnostic(exceptionVariable: exceptionVariable, syntaxNodeContext: syntaxNodeContext, location: location);
            return;
        }

        bool hasExceptionArgument = argumentList.Arguments.Any(argument =>
            argument.Expression is IdentifierNameSyntax identifier
            && StringComparer.Ordinal.Equals(x: identifier.Identifier.Text, y: exceptionVariable)
        );

        if (!hasExceptionArgument)
        {
            ReportDiagnostic(exceptionVariable: exceptionVariable, syntaxNodeContext: syntaxNodeContext, location: location);
        }
    }

    private static void ReportDiagnostic(
        string exceptionVariable,
        in SyntaxNodeAnalysisContext syntaxNodeContext,
        Location location
    )
    {
        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, location: location, exceptionVariable));
    }

    private static IReadOnlyList<ExpressionSyntax> GetAllThrowExpressions(BlockSyntax codeBlock)
    {
        IEnumerable<ExpressionSyntax> throwStatements = codeBlock.DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Select(x => x.Expression)
            .RemoveNulls();

        IEnumerable<ExpressionSyntax> throwExpressions = codeBlock.DescendantNodes()
            .OfType<ThrowExpressionSyntax>()
            .Select(x => x.Expression);

        return [.. throwStatements.Concat(throwExpressions)];
    }
}