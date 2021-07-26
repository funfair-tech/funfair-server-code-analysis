﻿using System.Collections.Immutable;
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
            if (syntaxNodeAnalysisContext.Node is not MethodDeclarationSyntax methodDeclarationSyntax)
            {
                return;
            }

            if (!TestDetection.IsTestMethod(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, methodDeclarationSyntax: methodDeclarationSyntax))
            {
                return;
            }

            if (!TestDetection.IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, methodDeclarationSyntax.GetLocation()));
            }
        }
    }
}