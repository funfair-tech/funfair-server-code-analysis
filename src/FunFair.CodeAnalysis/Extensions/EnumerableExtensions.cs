using System;
using System.Collections.Generic;

namespace FunFair.CodeAnalysis.Extensions;

public static class EnumerableExtensions
{
    public static void ForEach<TNode>(this IEnumerable<TNode> list, Action<TNode> action)
    {
        foreach (TNode item in list)
        {
            action(item);
        }
    }
}