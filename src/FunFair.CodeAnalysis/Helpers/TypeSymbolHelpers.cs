using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class TypeSymbolHelpers
{
    public static string? ToFullyQualifiedName(this ITypeSymbol symbol)
    {
        ITypeSymbol od = symbol.OriginalDefinition;
        INamespaceSymbol? ns = od.ContainingNamespace;

        if (ns is null)
        {
            return null;
        }

        string nameSpace = ns.ToDisplayString();

        return string.IsNullOrEmpty(nameSpace) ? null : $"{nameSpace}.{od.MetadataName}";
    }
}
