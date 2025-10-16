using System;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
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

        FindErrors(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            catchClause: catchClause,
            exceptionVariable: exceptionVariable
        );
    }

    private static void FindErrors(
        SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        CatchClauseSyntax catchClause,
        string exceptionVariable
    )
    {
        // ! argumentList is always not null here
        catchClause
            .Block.DescendantNodes()
            .Select(ThrowExpressionType)
            .RemoveNulls()
            .Select(ExpressionWithArgumentList)
            .Where(expression => expression.argumentList is not null)
            .ForEach(item =>
                CheckArgumentList(
                    item.argumentList!,
                    exceptionVariable: exceptionVariable,
                    syntaxNodeContext: syntaxNodeAnalysisContext,
                    item.expression.GetLocation()
                )
            );
    }

    private static (ExpressionSyntax expression, ArgumentListSyntax? argumentList) ExpressionWithArgumentList(
        ExpressionSyntax expression
    )
    {
        return (
            expression,
            argumentList: expression switch
            {
                ObjectCreationExpressionSyntax objectCreation => objectCreation.ArgumentList,
                InvocationExpressionSyntax invocation => invocation.ArgumentList,
                _ => null,
            }
        );
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
            ReportDiagnostic(
                exceptionVariable: exceptionVariable,
                syntaxNodeContext: syntaxNodeContext,
                location: location
            );

            return;
        }

        bool hasExceptionArgument = argumentList.Arguments.Any(argument =>
            argument.Expression is IdentifierNameSyntax identifier
            && StringComparer.Ordinal.Equals(x: identifier.Identifier.Text, y: exceptionVariable)
        );

        if (!hasExceptionArgument)
        {
            ReportDiagnostic(
                exceptionVariable: exceptionVariable,
                syntaxNodeContext: syntaxNodeContext,
                location: location
            );
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

    private static ExpressionSyntax? ThrowExpressionType(SyntaxNode syntaxNode)
    {
        return syntaxNode switch
        {
            ThrowStatementSyntax throwStatement => throwStatement.Expression,
            ThrowExpressionSyntax throwExpression => throwExpression.Expression,
            _ => null,
        };
    }
}
