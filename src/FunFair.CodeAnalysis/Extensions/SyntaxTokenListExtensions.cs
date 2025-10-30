using System;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

internal static class SyntaxTokenListExtensions
{
    public static bool Any(this in SyntaxTokenList syntaxTokenList, Func<SyntaxToken, bool> predicate)
    {
        return Enumerable.Any(syntaxTokenList, predicate);
    }
}
