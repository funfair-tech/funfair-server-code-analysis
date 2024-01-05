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

    [SuppressMessage(category: "Nullable.Extended.Analyzer", checkId: "NX0001: Suppression of NullForgiving operator is not required", Justification = "Required here")]
    private static readonly string SuppressMessageFullName = typeof(SuppressMessageAttribute).FullName!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(RuleMustHaveJustification, RuleMustNotHaveTodoJustification);

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

            if (!StringComparer.Ordinal.Equals(x: ti.Type?.MetadataName, y: sourceClassType.MetadataName))
            {
                return;
            }

            SeparatedSyntaxList<AttributeArgumentSyntax>? args = methodDeclarationSyntax.ArgumentList?.Arguments;

            AttributeArgumentSyntax? justification = args?.FirstOrDefault(k => IsJustificationAttribute(k));

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

        private static bool IsJustificationAttribute(AttributeArgumentSyntax k)
        {
            return StringComparer.Ordinal.Equals(x: k.NameEquals?.Name.Identifier.Text, y: "Justification");
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