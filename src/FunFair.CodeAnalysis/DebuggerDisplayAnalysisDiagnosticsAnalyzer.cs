using System;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DebuggerDisplayAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleRecordsShouldSpecifyDebuggerDisplay,
        category: Categories.Debugging,
        title: "Should have DebuggerDisplay attribute",
        message: "Should have DebuggerDisplay attribute"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(
            GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None
        );
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(
            action: RecordMustHaveDebuggerDisplayAttribute,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration
        );
    }

    private static void RecordMustHaveDebuggerDisplayAttribute(
        SyntaxNodeAnalysisContext syntaxNodeAnalysisContext
    )
    {
        switch (syntaxNodeAnalysisContext.Node)
        {
            case RecordDeclarationSyntax recordDeclarationSyntax:
                RecordMustHaveDebuggerDisplayAttribute(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    recordDeclarationSyntax: recordDeclarationSyntax
                );

                return;
            case StructDeclarationSyntax structDeclarationSyntax:
                RecordMustHaveDebuggerDisplayAttribute(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    structDeclarationSyntax: structDeclarationSyntax
                );

                return;
            default:
                // should never happen
                return;
        }
    }

    private static void RecordMustHaveDebuggerDisplayAttribute(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        StructDeclarationSyntax structDeclarationSyntax
    )
    {
        if (!structDeclarationSyntax.Modifiers.Any(SyntaxKind.RecordKeyword))
        {
            return;
        }

        if (
            !HasDebuggerDisplayAttribute(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                structDeclarationSyntax: structDeclarationSyntax
            )
        )
        {
            structDeclarationSyntax.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: Rule
            );
        }
    }

    private static void RecordMustHaveDebuggerDisplayAttribute(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        RecordDeclarationSyntax recordDeclarationSyntax
    )
    {
        if (
            !HasDebuggerDisplayAttribute(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                recordDeclarationSyntax: recordDeclarationSyntax
            )
        )
        {
            recordDeclarationSyntax.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: Rule
            );
        }
    }

    private static bool HasDebuggerDisplayAttribute(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        RecordDeclarationSyntax recordDeclarationSyntax
    )
    {
        return HasDebuggerDisplayAttribute(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            attributeLists: recordDeclarationSyntax.AttributeLists
        );
    }

    private static bool HasDebuggerDisplayAttribute(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        StructDeclarationSyntax structDeclarationSyntax
    )
    {
        return HasDebuggerDisplayAttribute(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            attributeLists: structDeclarationSyntax.AttributeLists
        );
    }

    private static bool HasDebuggerDisplayAttribute(
        SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        in SyntaxList<AttributeListSyntax> attributeLists
    )
    {
        return attributeLists
            .SelectMany(selector: al => al.Attributes)
            .Select(attribute =>
                syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(
                    attributeSyntax: attribute,
                    cancellationToken: syntaxNodeAnalysisContext.CancellationToken
                )
            )
            .Select(ti => ti.Type)
            .RemoveNulls()
            .Any(ti => IsDebuggerDisplayAttribute(ti.ToDisplayString()));
    }

    private static bool IsDebuggerDisplayAttribute(string fullName)
    {
        return StringComparer.Ordinal.Equals(
            x: fullName,
            y: "System.Diagnostics.DebuggerDisplayAttribute"
        );
    }
}
