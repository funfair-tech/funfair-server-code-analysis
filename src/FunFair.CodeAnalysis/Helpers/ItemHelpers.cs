using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class ItemHelpers
{
    [SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S3267: Use Linq", Justification = "Not here")]
    public static IEnumerable<TItemType> RemoveNulls<TItemType>(this IEnumerable<TItemType?> source)
        where TItemType : class
    {
        foreach (TItemType? item in source)
        {
            if (item is not null)
            {
                yield return item;
            }
        }
    }
}
