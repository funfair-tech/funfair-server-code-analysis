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

        if (syntaxNodeAnalysisContext.ContainingSymbol is not INamedTypeSymbol containingType)
        {
            return;
        }

        IReadOnlyList<INamedTypeSymbol> baseClasses = [.. containingType.BaseClasses()];

        if (baseClasses.Count == 0)
        {
            return;
        }

        foreach (ConfiguredClass classDefinition in Classes
                     .Where(classDefinition =>
                                classDefinition.TypeMatchesClass(baseClasses: baseClasses)
                                && !classDefinition.HasCorrectClassModifier(classDeclarationSyntax: classDeclarationSyntax)
                                )
                 )
        {
            classDeclarationSyntax.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: classDefinition.Rule
            );
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
        private static readonly StringComparer ClassNameComparer = StringComparer.Ordinal;

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

        public bool TypeMatchesClass(IReadOnlyList<INamedTypeSymbol> baseClasses)
        {
            string className = this.ClassName;

            return baseClasses.Any(baseClass => IsMatchingClass(typeSymbol: baseClass, className: className));
        }

        private static bool IsMatchingClass(INamedTypeSymbol typeSymbol, string className)
        {
            INamedTypeSymbol originalDefinition = typeSymbol.OriginalDefinition;
            string displayString = SymbolDisplay.ToDisplayString(originalDefinition);

            return ClassNameComparer.Equals(x: displayString, y: className);
        }

        public bool HasCorrectClassModifier(ClassDeclarationSyntax classDeclarationSyntax)
        {
            SyntaxKind visibility = this.Visibility;

            return classDeclarationSyntax.Modifiers.Any(modifier => modifier.IsKind(visibility));
        }
    }
}