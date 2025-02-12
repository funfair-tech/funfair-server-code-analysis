using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

internal static class SyntaxTokenListExtensions
{
    [SuppressMessage(category: "ReSharper", checkId: "ForCanBeConvertedToForeach", Justification = "Avoids boxing")]
    public static bool Any(this in SyntaxTokenList syntaxTokenList, Func<SyntaxToken, bool> predicate)
    {
        for (int i = 0; i < syntaxTokenList.Count; i++)
        {
            SyntaxToken syntaxToken = syntaxTokenList[i];

            if (predicate(syntaxToken))
            {
                return true;
            }
        }

        return false;
    }
}
