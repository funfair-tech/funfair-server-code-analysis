using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Extensions;

internal static class SyntaxNodeExtensions
{
    public static void ReportDiagnostics(this SyntaxNode expressionSyntax, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, DiagnosticDescriptor rule)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(CreateDiagnostic(expressionSyntax: expressionSyntax, rule: rule));
    }

    public static void ReportDiagnostics(this SyntaxNode expressionSyntax, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, DiagnosticDescriptor rule, params object?[]? messageArgs)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(CreateDiagnostic(expressionSyntax: expressionSyntax, rule: rule, messageArgs: messageArgs));
    }

    private static Diagnostic CreateDiagnostic(SyntaxNode expressionSyntax, DiagnosticDescriptor rule)
    {
        return Diagnostic.Create(descriptor: rule, expressionSyntax.GetLocation());
    }

    private static Diagnostic CreateDiagnostic(SyntaxNode expressionSyntax, DiagnosticDescriptor rule, object?[]? messageArgs)
    {
        return messageArgs is null || messageArgs.Length == 0
            ? CreateDiagnostic(expressionSyntax: expressionSyntax, rule: rule)
            : Diagnostic.Create(descriptor: rule, expressionSyntax.GetLocation(), messageArgs: messageArgs);
    }
}