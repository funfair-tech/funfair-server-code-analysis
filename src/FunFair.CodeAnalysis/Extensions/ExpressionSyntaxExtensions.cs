using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Extensions;

internal static class ExpressionSyntaxExtensions
{
    public static void ReportDiagnostics(this SyntaxNode expressionSyntax, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, DiagnosticDescriptor rule)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule, expressionSyntax.GetLocation()));
    }

    public static void ReportDiagnostics(this SyntaxNode expressionSyntax, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, DiagnosticDescriptor rule, params object?[]? messageArgs)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule, expressionSyntax.GetLocation(), messageArgs: messageArgs));
    }
}