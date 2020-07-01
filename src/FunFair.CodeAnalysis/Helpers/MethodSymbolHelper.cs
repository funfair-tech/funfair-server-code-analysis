using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers
{
    /// <summary>
    ///     Method symbol helper
    /// </summary>
    public static class MethodSymbolHelper
    {
        /// <summary>
        ///     Find invoked member symbol
        /// </summary>
        /// <param name="invocation">Invocation expression syntax</param>
        /// <param name="syntaxNodeAnalysisContext">Syntax node analysis context</param>
        /// <returns></returns>
        public static IMethodSymbol? FindInvokedMemberSymbol(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            MemberAccessExpressionSyntax? memberAccessExpressionSyntax = invocation.Expression as MemberAccessExpressionSyntax;

            if (memberAccessExpressionSyntax == null)
            {
                return null;
            }

            IMethodSymbol? memberSymbol = syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(node: memberAccessExpressionSyntax)
                                                                   .Symbol as IMethodSymbol;

            return memberSymbol;
        }
    }
}