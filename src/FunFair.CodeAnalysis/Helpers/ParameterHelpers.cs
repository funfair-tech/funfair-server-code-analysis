using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers;

internal static class ParameterHelpers
{
    public static string? GetFullTypeName(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        ParameterSyntax parameterSyntax,
        CancellationToken cancellationToken
    )
    {
        IParameterSymbol? ds = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(
            declarationSyntax: parameterSyntax,
            cancellationToken: cancellationToken
        );

        if (ds is not null)
        {
            ITypeSymbol typeSymbol = GetTypeSymbol(ds);

            return typeSymbol.ToDisplayString();
        }

        return null;
    }

    private static ITypeSymbol GetTypeSymbol(IParameterSymbol ds)
    {
        ITypeSymbol dsType = ds.Type;

        if (dsType is INamedTypeSymbol { IsGenericType: true })
        {
            dsType = dsType.OriginalDefinition;
        }

        return dsType;
    }
}
