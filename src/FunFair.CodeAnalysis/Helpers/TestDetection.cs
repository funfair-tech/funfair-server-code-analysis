using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers;

internal static class TestDetection
{
    public static bool IsTestMethod(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, MethodDeclarationSyntax methodDeclarationSyntax)
    {
        return methodDeclarationSyntax.AttributeLists.SelectMany(selector: al => al.Attributes)
                                      .Select(attribute => syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(attributeSyntax: attribute,
                                                                                                               cancellationToken: syntaxNodeAnalysisContext.CancellationToken))
                                      .Select(ti => ti.Type)
                                      .RemoveNulls()
                                      .Any(ti => IsTestMethodAttribute(ti.ToDisplayString()));
    }

    public static bool IsDerivedFromTestBase(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        ISymbol? containingType = syntaxNodeAnalysisContext.ContainingSymbol;

        if (containingType is null)
        {
            return false;
        }

        return containingType.ContainingType.BaseClasses()
                             .Any(IsTestBase);
    }

    private static bool IsTestBase(INamedTypeSymbol symbol)
    {
        return symbol.ToFullyQualifiedName() == "FunFair.Test.Common.TestBase";
    }

    private static bool IsTestMethodAttribute(string attributeType)
    {
        return StringComparer.InvariantCultureIgnoreCase.Equals(x: attributeType, y: @"Xunit.FactAttribute") ||
               StringComparer.InvariantCultureIgnoreCase.Equals(x: attributeType, y: @"Xunit.TheoryAttribute");
    }
}