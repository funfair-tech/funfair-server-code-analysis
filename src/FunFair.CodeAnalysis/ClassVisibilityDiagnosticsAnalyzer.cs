using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ClassVisibilityDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<ConfiguredClass> Classes =
    [
        Build(
            ruleId: Rules.MockBaseClassInstancesMustBeInternal,
            title: "MockBase<T> instances must be internal",
            message: "MockBase<T> instances must be internal",
            className: "FunFair.Test.Common.Mocks.MockBase<T>",
            visibility: SyntaxKind.InternalKeyword
        ),
        Build(
            ruleId: Rules.MockBaseClassInstancesMustBeSealed,
            title: "MockBase<T> instances must be sealed",
            message: "MockBase<T> instances must be sealed",
            className: "FunFair.Test.Common.Mocks.MockBase<T>",
            visibility: SyntaxKind.SealedKeyword
        ),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. Classes.Select(c => c.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: CheckClassVisibility, SyntaxKind.ClassDeclaration);
    }

    private static void CheckClassVisibility(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not ClassDeclarationSyntax classDeclarationSyntax)
        {
            return;
        }

        foreach (ConfiguredClass classDefinition in Classes)
        {
            if (
                classDefinition.TypeMatchesClass(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext)
                && !classDefinition.HasCorrectClassModifier(classDeclarationSyntax: classDeclarationSyntax)
            )
            {
                classDeclarationSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: classDefinition.Rule
                );
            }
        }
    }

    private static ConfiguredClass Build(
        string ruleId,
        string title,
        string message,
        string className,
        SyntaxKind visibility
    )
    {
        return new(ruleId: ruleId, title: title, message: message, className: className, visibility: visibility);
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {ClassName} Visibility {Visibility}")]
    private readonly record struct ConfiguredClass
    {
        public ConfiguredClass(string ruleId, string title, string message, string className, SyntaxKind visibility)
        {
            this.ClassName = className;
            this.Visibility = visibility;
            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.Classes,
                title: title,
                message: message
            );
        }

        public DiagnosticDescriptor Rule { get; }

        private string ClassName { get; }

        private SyntaxKind Visibility { get; }

        public bool TypeMatchesClass(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.ContainingSymbol is not INamedTypeSymbol containingType)
            {
                return false;
            }

            string className = this.ClassName;

            return containingType.BaseClasses().Any(s => IsMatchingClass(typeSymbol: s, className: className));
        }

        private static bool IsMatchingClass(INamedTypeSymbol typeSymbol, string className)
        {
            INamedTypeSymbol originalDefinition = typeSymbol.OriginalDefinition;

            return StringComparer.Ordinal.Equals(SymbolDisplay.ToDisplayString(originalDefinition), y: className);
        }

        public bool HasCorrectClassModifier(ClassDeclarationSyntax classDeclarationSyntax)
        {
            SyntaxKind visibility = this.Visibility;

            return classDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(visibility));
        }
    }
}
