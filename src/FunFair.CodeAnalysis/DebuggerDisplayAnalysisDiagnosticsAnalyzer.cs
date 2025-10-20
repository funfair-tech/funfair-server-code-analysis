using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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
    private const string DEBUGGER_DISPLAY_ATTRIBUTE_FULL_NAME = "System.Diagnostics.DebuggerDisplayAttribute";

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleRecordsShouldSpecifyDebuggerDisplay,
                                                                               category: Categories.Debugging,
                                                                               title: "Should have DebuggerDisplay attribute",
                                                                               message: "Should have DebuggerDisplay attribute");

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache = SupportedDiagnosisList.Build(Rule);

    private static readonly StringComparer AttributeComparer = StringComparer.Ordinal;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: RecordMustHaveDebuggerDisplayAttribute, SyntaxKind.RecordDeclaration, SyntaxKind.RecordStructDeclaration);
    }

    [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231:Make parameter ref read only", Justification = "Needed here")]
    public static void RecordMustHaveDebuggerDisplayAttribute(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        switch (syntaxNodeAnalysisContext.Node)
        {
            case RecordDeclarationSyntax recordDeclarationSyntax: CheckRecordDeclaration(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, recordDeclarationSyntax: recordDeclarationSyntax); break;

            case StructDeclarationSyntax structDeclarationSyntax: CheckStructDeclaration(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, structDeclarationSyntax: structDeclarationSyntax); break;
        }
    }

    private static void CheckRecordDeclaration(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, RecordDeclarationSyntax recordDeclarationSyntax)
    {
        if (!HasDebuggerDisplayAttribute(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, attributeLists: recordDeclarationSyntax.AttributeLists))
        {
            recordDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
        }
    }

    private static void CheckStructDeclaration(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, StructDeclarationSyntax structDeclarationSyntax)
    {
        if (!IsRecordStruct(structDeclarationSyntax))
        {
            return;
        }

        if (!HasDebuggerDisplayAttribute(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, attributeLists: structDeclarationSyntax.AttributeLists))
        {
            structDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
        }
    }

    private static bool IsRecordStruct(StructDeclarationSyntax structDeclarationSyntax)
    {
        return structDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RecordKeyword));
    }

    private static bool HasDebuggerDisplayAttribute(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, in SyntaxList<AttributeListSyntax> attributeLists)
    {
        return attributeLists.SelectMany(al => al.Attributes)
                             .Select(attribute => GetAttributeTypeInfo(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, attribute: attribute))
                             .RemoveNulls()
                             .Any(IsDebuggerDisplayAttribute);
    }

    private static ITypeSymbol? GetAttributeTypeInfo(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, AttributeSyntax attribute)
    {
        return syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(attributeSyntax: attribute, cancellationToken: syntaxNodeAnalysisContext.CancellationToken)
                                        .Type;
    }

    private static bool IsDebuggerDisplayAttribute(ITypeSymbol typeSymbol)
    {
        string fullName = typeSymbol.ToDisplayString();

        return IsDebuggerDisplayAttributeName(fullName);
    }

    private static bool IsDebuggerDisplayAttributeName(string fullName)
    {
        return AttributeComparer.Equals(x: fullName, y: DEBUGGER_DISPLAY_ATTRIBUTE_FULL_NAME);
    }
}