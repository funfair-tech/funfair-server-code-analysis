using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class SupportedDiagnosisList
{
    public static ImmutableArray<DiagnosticDescriptor> Build(DiagnosticDescriptor rule)
    {
        return ImmutableArray<DiagnosticDescriptor>.Empty.Add(item: rule);
    }

    public static ImmutableArray<DiagnosticDescriptor> Build(params DiagnosticDescriptor[] rules)
    {
        return [..rules];
    }

    public static ImmutableArray<DiagnosticDescriptor> Build(IEnumerable<DiagnosticDescriptor> rules)
    {
        return [..rules];
    }
}