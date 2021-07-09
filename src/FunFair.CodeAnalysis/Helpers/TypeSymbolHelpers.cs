using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers
{
    internal static class TypeSymbolHelpers
    {
        public static string ToFullyQualifiedName(this ITypeSymbol symbol)
        {
            string nameSpace = symbol.OriginalDefinition.ContainingNamespace.ToDisplayString();

            return $"{nameSpace}.{symbol.OriginalDefinition.MetadataName}";
        }
    }
}