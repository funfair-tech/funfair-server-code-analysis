using System;
using System.Collections.Generic;
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

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleRecordsShouldSpecifyDebuggerDisplay,
        category: Categories.Debugging,
        title: "Should have DebuggerDisplay attribute",
        message: "Should have DebuggerDisplay attribute"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

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
        Checker checker = new();

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.RecordMustHaveDebuggerDisplayAttribute,
            SyntaxKind.RecordDeclaration,
            SyntaxKind.RecordStructDeclaration
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<ITypeSymbol, bool> _attributeTypeCache = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<string, bool> _attributeNameCache = new(StringComparer.Ordinal);

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void RecordMustHaveDebuggerDisplayAttribute(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            switch (syntaxNodeAnalysisContext.Node)
            {
                case RecordDeclarationSyntax recordDeclarationSyntax:
                    this.CheckRecordDeclaration(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        recordDeclarationSyntax: recordDeclarationSyntax
                    );
                    break;

                case StructDeclarationSyntax structDeclarationSyntax:
                    this.CheckStructDeclaration(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        structDeclarationSyntax: structDeclarationSyntax
                    );
                    break;
            }
        }

        private void CheckRecordDeclaration(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            RecordDeclarationSyntax recordDeclarationSyntax
        )
        {
            if (
                !this.HasDebuggerDisplayAttribute(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    attributeLists: recordDeclarationSyntax.AttributeLists
                )
            )
            {
                recordDeclarationSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: Rule
                );
            }
        }

        private void CheckStructDeclaration(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            StructDeclarationSyntax structDeclarationSyntax
        )
        {
            if (!IsRecordStruct(structDeclarationSyntax))
            {
                return;
            }

            if (
                !this.HasDebuggerDisplayAttribute(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    attributeLists: structDeclarationSyntax.AttributeLists
                )
            )
            {
                structDeclarationSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: Rule
                );
            }
        }

        private static bool IsRecordStruct(StructDeclarationSyntax structDeclarationSyntax)
        {
            return structDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(SyntaxKind.RecordKeyword));
        }

        private bool HasDebuggerDisplayAttribute(
            SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            in SyntaxList<AttributeListSyntax> attributeLists
        )
        {
            return attributeLists
                .SelectMany(al => al.Attributes)
                .Select(attribute =>
                    GetAttributeTypeInfo(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, attribute: attribute)
                )
                .RemoveNulls()
                .Any(this.IsDebuggerDisplayAttribute);
        }

        private static ITypeSymbol? GetAttributeTypeInfo(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            AttributeSyntax attribute
        )
        {
            return syntaxNodeAnalysisContext
                .SemanticModel.GetTypeInfo(
                    attributeSyntax: attribute,
                    cancellationToken: syntaxNodeAnalysisContext.CancellationToken
                )
                .Type;
        }

        private bool IsDebuggerDisplayAttribute(ITypeSymbol typeSymbol)
        {
            if (this._attributeTypeCache.TryGetValue(key: typeSymbol, out bool isDebuggerDisplay))
            {
                return isDebuggerDisplay;
            }

            string fullName = typeSymbol.ToDisplayString();
            bool result = this.IsDebuggerDisplayAttributeName(fullName);

            this._attributeTypeCache[typeSymbol] = result;
            return result;
        }

        private bool IsDebuggerDisplayAttributeName(string fullName)
        {
            if (this._attributeNameCache.TryGetValue(key: fullName, out bool isMatch))
            {
                return isMatch;
            }

            bool result = AttributeComparer.Equals(x: fullName, y: DEBUGGER_DISPLAY_ATTRIBUTE_FULL_NAME);

            this._attributeNameCache[fullName] = result;
            return result;
        }
    }
}
