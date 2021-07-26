using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers
{
    internal static class TestDetection
    {
        public static bool IsTestMethod(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            return methodDeclarationSyntax.AttributeLists.SelectMany(selector: al => al.Attributes)
                                          .Select(attribute => syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(attribute))
                                          .Select(ti => ti.Type)
                                          .RemoveNulls()
                                          .Any(ti => IsTestMethodAttribute(ti.ToDisplayString()));
        }

        public static bool IsDerivedFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            ISymbol? containingType = syntaxNodeAnalysisContext.ContainingSymbol;

            if (containingType == null)
            {
                return false;
            }

            for (INamedTypeSymbol? parent = containingType.ContainingType; parent != null; parent = parent.BaseType)
            {
                if (SymbolDisplay.ToDisplayString(parent) == "FunFair.Test.Common.TestBase")
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTestMethodAttribute(string attributeType)
        {
            return StringComparer.InvariantCultureIgnoreCase.Equals(x: attributeType, y: @"Xunit.FactAttribute") ||
                   StringComparer.InvariantCultureIgnoreCase.Equals(x: attributeType, y: @"Xunit.TheoryAttribute");
        }
    }
}