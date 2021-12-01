using System.Collections.Generic;
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
    ///     Looks for catches which throw a new exception without passing the exception as an inner exception.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleMustPassInterExceptionToExceptionsThrownInCatchBlock,
                                                                                   category: Categories.Exceptions,
                                                                                   title: "Pass an a inner exception when thrown from a catch clause",
                                                                                   message: "Provide '{0}' as a inner exception when throw from the catch clauses");

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
            compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.CatchClause);
        }

        private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not CatchClauseSyntax catchClause)
            {
                return;
            }

            string? exceptionVariable = catchClause.Declaration?.Identifier.Text;

            if (exceptionVariable == null)
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
                    TryToReportDiagnostic(argumentListSyntax: objectCreationExpression.ArgumentList,
                                          exceptionVariable: exceptionVariable,
                                          syntaxNodeContext: syntaxNodeAnalysisContext,
                                          objectCreationExpression: objectCreationExpression);
                }
                else if (expression is InvocationExpressionSyntax invocationExpressionSyntax)
                {
                    TryToReportDiagnostic(argumentListSyntax: invocationExpressionSyntax.ArgumentList,
                                          exceptionVariable: exceptionVariable,
                                          syntaxNodeContext: syntaxNodeAnalysisContext,
                                          objectCreationExpression: invocationExpressionSyntax);
                }
            }
        }

        private static void TryToReportDiagnostic(ArgumentListSyntax? argumentListSyntax,
                                                  string exceptionVariable,
                                                  SyntaxNodeAnalysisContext syntaxNodeContext,
                                                  ExpressionSyntax objectCreationExpression)
        {
            if (argumentListSyntax == null || argumentListSyntax.Arguments.Count == 0)
            {
                ReportDiagnostic(exceptionVariable: exceptionVariable, syntaxNodeContext: syntaxNodeContext, objectCreationExpression.GetLocation());

                return;
            }

            if (!argumentListSyntax.Arguments.Any(x => x.Expression is IdentifierNameSyntax identifier && identifier.Identifier.Text == exceptionVariable))
            {
                ReportDiagnostic(exceptionVariable: exceptionVariable, syntaxNodeContext: syntaxNodeContext, objectCreationExpression.GetLocation());
            }
        }

        private static void ReportDiagnostic(string exceptionVariable, SyntaxNodeAnalysisContext syntaxNodeContext, Location location)
        {
            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, location: location, exceptionVariable));
        }

        private static IReadOnlyList<ExpressionSyntax> GetAllThrowExpressions(BlockSyntax codeBlock)
        {
            IEnumerable<ExpressionSyntax> expressionFromThrowStatements = codeBlock.DescendantNodes()
                                                                                   .OfType<ThrowStatementSyntax>()
                                                                                   .Select(x => x.Expression)
                                                                                   .RemoveNulls();
            IEnumerable<ExpressionSyntax> expressionFromThrowExpressions = codeBlock.DescendantNodes()
                                                                                    .OfType<ThrowExpressionSyntax>()
                                                                                    .Select(x => x.Expression);

            return expressionFromThrowStatements.Concat(expressionFromThrowExpressions)
                                                .ToArray();
        }
    }
}