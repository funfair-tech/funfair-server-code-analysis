using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for problems with <see cref="SuppressMessageAttribute" />
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SuppressMessageDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor RuleMustHaveJustification = RuleHelpers.CreateRule(code: Rules.RuleSuppressMessageMustHaveJustification,
                                                                                                    category: Categories.SuppressedErrors,
                                                                                                    title: "SuppressMessage must specify a Justification",
                                                                                                    message: "SuppressMessage must specify a Justification");

    private static readonly DiagnosticDescriptor RuleMustNotHaveTodoJustification = RuleHelpers.CreateRule(code: Rules.RuleSuppressMessageMustNotHaveTodoJustification,
                                                                                                           category: Categories.SuppressedErrors,
                                                                                                           title: "SuppressMessage must not have a TODO Justification",
                                                                                                           message: "SuppressMessage must not have a TODO Justification");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        new[]
        {
            RuleMustHaveJustification,
            RuleMustNotHaveTodoJustification
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
        INamedTypeSymbol? sourceClassType = compilationStartContext.Compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName);

        if (sourceClassType == null)
        {
            return;
        }

        compilationStartContext.RegisterSyntaxNodeAction(action: syntaxNodeAnalysisContext =>
                                                                     MustDeriveFromTestBase(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceClassType: sourceClassType),
                                                         SyntaxKind.Attribute);
    }

    private static void MustDeriveFromTestBase(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceClassType)
    {
        if (syntaxNodeAnalysisContext.Node is not AttributeSyntax methodDeclarationSyntax)
        {
            return;
        }

        TypeInfo ti = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(methodDeclarationSyntax.Name);

        if (ti.Type?.MetadataName != sourceClassType.MetadataName)
        {
            return;
        }

        AttributeArgumentSyntax? justification = methodDeclarationSyntax.ArgumentList?.Arguments.FirstOrDefault(k => k.NameEquals?.Name.Identifier.Text == "Justification");

        if (justification == null)
        {
            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, methodDeclarationSyntax: methodDeclarationSyntax);

            return;
        }

        if (justification.Expression is not LiteralExpressionSyntax l)
        {
            return;
        }

        string text = l.Token.ValueText;

        CheckJustification(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, text: text, l: l);
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, AttributeSyntax methodDeclarationSyntax)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: RuleMustHaveJustification, methodDeclarationSyntax.GetLocation()));
    }

    private static void CheckJustification(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, string text, LiteralExpressionSyntax l)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, l: l, rule: RuleMustHaveJustification);

            return;
        }

        if (text.StartsWith(value: "TODO", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, l: l, rule: RuleMustNotHaveTodoJustification);
        }
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, LiteralExpressionSyntax l, DiagnosticDescriptor rule)
    {
        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule, l.GetLocation()));
    }
}