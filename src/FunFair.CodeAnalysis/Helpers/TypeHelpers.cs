using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers
{
    internal static class TypeHelpers
    {
        public static IEnumerable<INamedTypeSymbol> BaseClasses(this ISymbol containingType)
        {
            for (INamedTypeSymbol? parent = containingType.ContainingType; parent != null; parent = parent.BaseType)
            {
                yield return parent;
            }
        }
    }
}