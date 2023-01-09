using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers;

/// <summary>
///     Method symbol helper
/// </summary>
internal static class MethodSymbolHelper
{
    /// <summary>
    ///     Find invoked member symbol
    /// </summary>
    /// <param name="invocation">Invocation expression syntax</param>
    /// <param name="syntaxNodeAnalysisContext">Syntax node analysis context</param>
    /// <returns></returns>
    public static IMethodSymbol? FindInvokedMemberSymbol(InvocationExpressionSyntax invocation, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (invocation.Expression is MemberAccessExpressionSyntax memberAccessExpressionSyntax)
        {
            return GetSimpleMemberSymbol(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, memberAccessExpressionSyntax: memberAccessExpressionSyntax) ??
                   ResolveExtensionMethodUsedByConstructor(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, memberAccessExpressionSyntax: memberAccessExpressionSyntax);
        }

        return null;
    }

    private static IMethodSymbol? GetSimpleMemberSymbol(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        return GetSymbol(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, expression: memberAccessExpressionSyntax) as IMethodSymbol;
    }

    private static IMethodSymbol? ResolveExtensionMethodUsedByConstructor(InvocationExpressionSyntax invocation,
                                                                          in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
                                                                          MemberAccessExpressionSyntax memberAccessExpressionSyntax)
    {
        INamedTypeSymbol? sourceType = GetSourceType(memberAccessExpressionSyntax: memberAccessExpressionSyntax, semanticModel: syntaxNodeAnalysisContext.SemanticModel);

        if (sourceType == null)
        {
            return null;
        }

        string fullName = memberAccessExpressionSyntax.Name.ToFullString();
        ImmutableArray<ISymbol> symbols = BuildSymbols(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceType: sourceType, fullName: fullName);

        return symbols.OfType<IMethodSymbol>()
                      .FirstOrDefault(sym => HasMatchingArguments(invocation: invocation, arguments: sym));
    }

    private static ImmutableArray<ISymbol> BuildSymbols(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceType, string fullName)
    {
        ImmutableArray<ISymbol> symbols = BuildSymbolsWithBaseTypes(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceType: sourceType, fullName: fullName);

        Dump(symbols);

        return symbols;
    }

    private static ImmutableArray<ISymbol> BuildSymbolsWithBaseTypes(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceType, string fullName)
    {
        return LookupSymbols(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceType: sourceType, fullName: fullName)
               .Concat(sourceType.BaseClasses()
                                 .SelectMany(baseType => LookupSymbols(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, fullName: fullName, sourceType: baseType)))
               .Concat(sourceType.AllInterfaces.SelectMany(interfaceType => LookupSymbols(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceType: interfaceType, fullName: fullName)))
               .ToImmutableArray();
    }

    private static ImmutableArray<ISymbol> LookupSymbols(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceType, string fullName)
    {
        return syntaxNodeAnalysisContext.SemanticModel.LookupSymbols(position: 0, container: sourceType, name: fullName, includeReducedExtensionMethods: true);
    }

    private static bool HasMatchingArguments(InvocationExpressionSyntax invocation, IMethodSymbol arguments)
    {
        // Ideally: Match on something more than just the count of methods - i.e. match on types and argument names?
        // It is hard to make any match because we don't know for sure to which parameter argument is related.
        // Argument may or may not have argument name - this is optional
        // We can't compare lists by types user in parameters, order in invocation list can be different than in parameter list
        // due to use of parameter names (then order is not relevant).
        return arguments.Parameters.Length == invocation.ArgumentList.Arguments.Count;
    }

    [Conditional("DEBUG")]
    private static void Dump(in ImmutableArray<ISymbol> symbols)
    {
        foreach (ISymbol symbol in symbols)
        {
            Debug.WriteLine(symbol.ToDisplayString());
        }
    }

    private static ISymbol? GetSymbol(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, SyntaxNode expression)
    {
        return syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(node: expression)
                                        .Symbol;
    }

    private static INamedTypeSymbol? GetSourceType(MemberAccessExpressionSyntax memberAccessExpressionSyntax, SemanticModel semanticModel)
    {
        ISymbol? symbol = semanticModel.GetSymbolInfo(memberAccessExpressionSyntax.Expression)
                                       .Symbol;

        return symbol switch
        {
            ILocalSymbol local => local.Type,
            IParameterSymbol param => param.Type,
            IFieldSymbol field => field.Type,
            IPropertySymbol prop => prop.Type,
            IMethodSymbol method => method.MethodKind == MethodKind.Constructor
                ? method.ReceiverType
                : method.ReturnType,
            _ => null
        } as INamedTypeSymbol;
    }
}