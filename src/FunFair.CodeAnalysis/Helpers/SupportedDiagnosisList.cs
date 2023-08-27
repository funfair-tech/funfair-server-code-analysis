using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class SupportedDiagnosisList
{
    public static ImmutableArray<DiagnosticDescriptor> Build(DiagnosticDescriptor rule)
    {
        return new[]
               {
                   rule
               }.ToImmutableArray();
    }

    public static ImmutableArray<DiagnosticDescriptor> Build(params DiagnosticDescriptor[] rules)
    {
        return rules.ToImmutableArray();
    }
}