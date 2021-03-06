﻿using System;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with class declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class TestClassAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Classes";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleTestClassesShouldBeStaticSealedOrAbstractDerivedFromTestBase,
                                                                                   category: CATEGORY,
                                                                                   title: "Test classes should be derived from TestBase",
                                                                                   message: "Test classes should be derived from TestBase");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: MustDeriveFromTestBase, SyntaxKind.MethodDeclaration);
        }

        private static void MustDeriveFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (!(syntaxNodeAnalysisContext.Node is MethodDeclarationSyntax methodDeclarationSyntax))
            {
                return;
            }

            if (!IsTestMethod(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, methodDeclarationSyntax: methodDeclarationSyntax))
            {
                return;
            }

            if (!IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, methodDeclarationSyntax.GetLocation()));
            }
        }

        private static bool IsDerivedFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
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

        private static bool IsTestMethod(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, MethodDeclarationSyntax methodDeclarationSyntax)
        {
            foreach (AttributeSyntax attribute in methodDeclarationSyntax.AttributeLists.SelectMany(selector: al => al.Attributes))
            {
                TypeInfo ti = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(attribute);

                if (ti.Type != null)
                {
                    if (IsTestMethodAttribute(ti.Type.ToDisplayString()))
                    {
                        return true;
                    }
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