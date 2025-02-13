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
public sealed class ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzer
    : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleMustPassInterExceptionToExceptionsThrownInCatchBlock,
        category: Categories.Exceptions,
        title: "Pass an a inner exception when thrown from a catch clause",
        message: "Provide '{0}' as a inner exception when throw from the catch clauses"
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
            action: MustBeReadOnly,
            SyntaxKind.CatchClause
        );
    }

    private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not CatchClauseSyntax catchClause)
        {
            return;
        }

        string? exceptionVariable = GetExceptionVariable(catchClause);

        if (exceptionVariable is null)
        {
            return;
        }

        IReadOnlyList<ExpressionSyntax> allExpressions = GetAllThrowExpressions(catchClause.Block);

        if (allExpressions.Count == 0)
        {
            return;
        }

        foreach (ExpressionSyntax expression in allExpressions)
        {
            if (expression is ObjectCreationExpressionSyntax objectCreationExpression)
            {
                TryToReportDiagnostic(
                    argumentListSyntax: objectCreationExpression.ArgumentList,
                    exceptionVariable: exceptionVariable,
                    syntaxNodeContext: syntaxNodeAnalysisContext,
                    objectCreationExpression: objectCreationExpression
                );
            }
            else if (expression is InvocationExpressionSyntax invocationExpressionSyntax)
            {
                TryToReportDiagnostic(
                    argumentListSyntax: invocationExpressionSyntax.ArgumentList,
                    exceptionVariable: exceptionVariable,
                    syntaxNodeContext: syntaxNodeAnalysisContext,
                    objectCreationExpression: invocationExpressionSyntax
                );
            }
        }
    }

    private static string? GetExceptionVariable(CatchClauseSyntax catchClause)
    {
        return catchClause.Declaration?.Identifier.Text;
    }

    private static void TryToReportDiagnostic(
        ArgumentListSyntax? argumentListSyntax,
        string exceptionVariable,
        in SyntaxNodeAnalysisContext syntaxNodeContext,
        ExpressionSyntax objectCreationExpression
    )
    {
        if (argumentListSyntax is null || argumentListSyntax.Arguments.Count == 0)
        {
            ReportDiagnostic(
                exceptionVariable: exceptionVariable,
                syntaxNodeContext: syntaxNodeContext,
                objectCreationExpression.GetLocation()
            );

            return;
        }

        if (
            !argumentListSyntax.Arguments.Any(x =>
                IsNamedIdentifier(exceptionVariable: exceptionVariable, x: x)
            )
        )
        {
            ReportDiagnostic(
                exceptionVariable: exceptionVariable,
                syntaxNodeContext: syntaxNodeContext,
                objectCreationExpression.GetLocation()
            );
        }
    }

    private static bool IsNamedIdentifier(string exceptionVariable, ArgumentSyntax x)
    {
        return x.Expression is IdentifierNameSyntax identifier
            && StringComparer.Ordinal.Equals(x: identifier.Identifier.Text, y: exceptionVariable);
    }

    private static void ReportDiagnostic(
        string exceptionVariable,
        in SyntaxNodeAnalysisContext syntaxNodeContext,
        Location location
    )
    {
        syntaxNodeContext.ReportDiagnostic(
            Diagnostic.Create(descriptor: Rule, location: location, exceptionVariable)
        );
    }

    private static IReadOnlyList<ExpressionSyntax> GetAllThrowExpressions(BlockSyntax codeBlock)
    {
        IEnumerable<ExpressionSyntax> expressionFromThrowStatements = ThrowStatements(codeBlock);
        IEnumerable<ExpressionSyntax> expressionFromThrowExpressions = ThrowExpressions(codeBlock);

        return [.. expressionFromThrowStatements.Concat(expressionFromThrowExpressions)];
    }

    private static IEnumerable<ExpressionSyntax> ThrowExpressions(BlockSyntax codeBlock)
    {
        return codeBlock
            .DescendantNodes()
            .OfType<ThrowExpressionSyntax>()
            .Select(x => x.Expression);
    }

    private static IEnumerable<ExpressionSyntax> ThrowStatements(BlockSyntax codeBlock)
    {
        return codeBlock
            .DescendantNodes()
            .OfType<ThrowStatementSyntax>()
            .Select(x => x.Expression)
            .RemoveNulls();
    }
}
