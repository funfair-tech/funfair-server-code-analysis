using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunFair.CodeAnalysis.Helpers;

internal static class BenchmarkDetection
{
    public static bool ClassHasBenchmarkMethods(ClassDeclarationSyntax classDeclaration)
    {
        return classDeclaration
            .Members.OfType<MethodDeclarationSyntax>()
            .Any(m => HasBenchmarkAttribute(m.AttributeLists));
    }

    public static bool MethodHasBenchmarkAttribute(MethodDeclarationSyntax method)
    {
        return HasBenchmarkAttribute(method.AttributeLists);
    }

    private static bool HasBenchmarkAttribute(in SyntaxList<AttributeListSyntax> attributeLists)
    {
        return attributeLists.SelectMany(al => al.Attributes).Any(IsBenchmarkAttribute);
    }

    private static bool IsBenchmarkAttribute(AttributeSyntax attribute)
    {
        string name = attribute.Name switch
        {
            IdentifierNameSyntax id => id.Identifier.Text,
            QualifiedNameSyntax qualified => qualified.Right.Identifier.Text,
            _ => attribute.Name.ToString(),
        };

        return StringComparer.Ordinal.Equals(x: name, y: "Benchmark")
            || StringComparer.Ordinal.Equals(x: name, y: "BenchmarkAttribute");
    }
}
