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

    private static readonly string SuppressMessageFullName = typeof(SuppressMessageAttribute).FullName!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        new[]
        {
            RuleMustHaveJustification,
            RuleMustNotHaveTodoJustification
        }.ToImmutableArray();

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();

        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private sealed class Checker
    {
        private INamedTypeSymbol? _suppressMessage;

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol? sourceClassType = this.GetSuppressMessageAttributeType(compilationStartContext.Compilation);

            if (sourceClassType is null)
            {
                return;
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: syntaxNodeAnalysisContext =>
                                                                         MustDeriveFromTestBase(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, sourceClassType: sourceClassType),
                                                             SyntaxKind.Attribute);
        }

        private INamedTypeSymbol? GetSuppressMessageAttributeType(Compilation compilation)
        {
            return this._suppressMessage ??= compilation.GetTypeByMetadataName(SuppressMessageFullName);
        }

        private static void MustDeriveFromTestBase(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceClassType)
        {
            if (syntaxNodeAnalysisContext.Node is not AttributeSyntax methodDeclarationSyntax)
            {
                return;
            }

            TypeInfo ti = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(expression: methodDeclarationSyntax.Name, cancellationToken: syntaxNodeAnalysisContext.CancellationToken);

            if (ti.Type?.MetadataName != sourceClassType.MetadataName)
            {
                return;
            }

            AttributeArgumentSyntax? justification = methodDeclarationSyntax.ArgumentList?.Arguments.FirstOrDefault(k => k.NameEquals?.Name.Identifier.Text == "Justification");

            if (justification is null)
            {
                methodDeclarationSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: RuleMustHaveJustification);

                return;
            }

            if (justification.Expression is not LiteralExpressionSyntax l)
            {
                return;
            }

            string text = l.Token.ValueText;

            CheckJustification(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, text: text, l: l);
        }

        private static void CheckJustification(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, string text, LiteralExpressionSyntax l)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                l.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: RuleMustHaveJustification);

                return;
            }

            if (text.StartsWith(value: "TODO", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                l.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: RuleMustNotHaveTodoJustification);
            }
        }
    }
}