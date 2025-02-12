using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class TypeHelpers
{
    public static IEnumerable<INamedTypeSymbol> BaseClasses(this INamedTypeSymbol sourceType)
    {
        for (INamedTypeSymbol? parent = sourceType; parent is not null; parent = parent.BaseType)
        {
            yield return parent;
        }
    }
}
