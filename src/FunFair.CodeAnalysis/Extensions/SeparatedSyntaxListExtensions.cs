using System;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

public static class SeparatedSyntaxListExtensions
{
    public static void ForEach<TNode>(in this SeparatedSyntaxList<TNode> list, Action<TNode> action)
        where TNode : SyntaxNode
    {
        foreach (TNode item in list)
        {
            action(item);
        }
    }
}
