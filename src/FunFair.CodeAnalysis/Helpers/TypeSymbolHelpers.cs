using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers
{
    internal static class TypeSymbolHelpers
    {
        public static string? ToFullyQualifiedName(this ITypeSymbol symbol)
        {
            if (symbol.OriginalDefinition.ContainingNamespace == null)
            {
                return null;
            }

            string nameSpace = symbol.OriginalDefinition.ContainingNamespace.ToDisplayString();

            if (string.IsNullOrEmpty(nameSpace))
            {
                return null;
            }

            return $"{nameSpace}.{symbol.OriginalDefinition.MetadataName}";
        }
    }
}