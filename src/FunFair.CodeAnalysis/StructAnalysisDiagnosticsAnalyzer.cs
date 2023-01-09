﻿using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for problems with structs.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class StructAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleStructsShouldBeReadOnly,
                                                                               category: Categories.Structs,
                                                                               title: "Structs should be read-only",
                                                                               message: "Structs should be read-only");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        new[]
        {
            Rule
        }.ToImmutableArray();

    /// <inheritdoc />
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.StructDeclaration);
    }

    private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not StructDeclarationSyntax structDeclarationSyntax)
        {
            return;
        }

        if (!structDeclarationSyntax.Modifiers.Any(IsReadOnlyKeyword))
        {
            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, structDeclarationSyntax: structDeclarationSyntax);
        }
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, StructDeclarationSyntax structDeclarationSyntax)
    {
        structDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
    }

    private static bool IsReadOnlyKeyword(SyntaxToken syntaxToken)
    {
        return syntaxToken.IsKind(SyntaxKind.ReadOnlyKeyword);
    }
}