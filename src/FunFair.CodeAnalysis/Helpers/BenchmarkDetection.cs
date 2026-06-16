using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunFair.CodeAnalysis.Helpers;

internal static class BenchmarkDetection
{
    private const string BENCHMARK_ATTRIBUTE_NAME = "BenchmarkAttribute";
    private const string BENCHMARK_DOTNET_ATTRIBUTES_NAMESPACE = "BenchmarkDotNet.Attributes";

    public static bool ClassHasBenchmarkMethods(
        ClassDeclarationSyntax classDeclaration,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        return classDeclaration
            .Members.OfType<MethodDeclarationSyntax>()
            .Any(m =>
                HasBenchmarkAttribute(
                    m.AttributeLists,
                    semanticModel: semanticModel,
                    cancellationToken: cancellationToken
                )
            );
    }

    public static bool MethodHasBenchmarkAttribute(
        MethodDeclarationSyntax method,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        return HasBenchmarkAttribute(
            method.AttributeLists,
            semanticModel: semanticModel,
            cancellationToken: cancellationToken
        );
    }

    private static bool HasBenchmarkAttribute(
        in SyntaxList<AttributeListSyntax> attributeLists,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        return attributeLists
            .SelectMany(al => al.Attributes)
            .Any(a =>
                IsBenchmarkAttribute(attribute: a, semanticModel: semanticModel, cancellationToken: cancellationToken)
            );
    }

    private static bool IsBenchmarkAttribute(
        AttributeSyntax attribute,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        TypeInfo typeInfo = semanticModel.GetTypeInfo(attribute, cancellationToken: cancellationToken);
        ITypeSymbol? type = typeInfo.Type;

        if (type is null)
        {
            return false;
        }

        return StringComparer.Ordinal.Equals(x: type.Name, y: BENCHMARK_ATTRIBUTE_NAME)
            && StringComparer.Ordinal.Equals(
                x: type.ContainingNamespace?.ToDisplayString(),
                y: BENCHMARK_DOTNET_ATTRIBUTES_NAMESPACE
            );
    }
}
