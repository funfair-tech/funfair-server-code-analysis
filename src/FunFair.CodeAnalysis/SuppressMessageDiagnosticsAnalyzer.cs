
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
    private static readonly DiagnosticDescriptor RuleMustHaveJustification = RuleHelpers.CreateRule(
        code: Rules.RuleSuppressMessageMustHaveJustification,
        category: Categories.SuppressedErrors,
        title: "SuppressMessage must specify a Justification",
        message: "SuppressMessage must specify a Justification"
    );

    private static readonly DiagnosticDescriptor RuleMustNotHaveTodoJustification = RuleHelpers.CreateRule(
        code: Rules.RuleSuppressMessageMustNotHaveTodoJustification,
        category: Categories.SuppressedErrors,
        title: "SuppressMessage must not have a TODO Justification",
        message: "SuppressMessage must not have a TODO Justification"
    );

    [SuppressMessage(
        category: "Nullable.Extended.Analyzer",
        checkId: "NX0001: Suppression of NullForgiving operator is not required",
        Justification = "Required here"
    )]
    private static readonly string SuppressMessageFullName = typeof(SuppressMessageAttribute).FullName!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        SupportedDiagnosisList.Build(RuleMustHaveJustification, RuleMustNotHaveTodoJustification);

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

            compilationStartContext.RegisterSyntaxNodeAction(
                action: syntaxNodeAnalysisContext =>
                    CheckSuppressMessageAttribute(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        sourceClassType: sourceClassType
                    ),
                SyntaxKind.Attribute
            );
        }

        private INamedTypeSymbol? GetSuppressMessageAttributeType(Compilation compilation)
        {
            return this._suppressMessage ??= compilation.GetTypeByMetadataName(SuppressMessageFullName);
        }

        private static void CheckSuppressMessageAttribute(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            INamedTypeSymbol sourceClassType
        )
        {
            if (syntaxNodeAnalysisContext.Node is not AttributeSyntax attributeSyntax)
            {
                return;
            }

            TypeInfo typeInfo = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(
                expression: attributeSyntax.Name,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

            if (!StringComparer.Ordinal.Equals(x: typeInfo.Type?.MetadataName, y: sourceClassType.MetadataName))
            {
                return;
            }

            AttributeArgumentSyntax? justificationAttributeArguement = FindJustificationAttributeArgument(attributeSyntax);

            if (justificationAttributeArguement is null)
            {
                attributeSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: RuleMustHaveJustification
                );
                return;
            }

            if (justificationAttributeArguement.Expression is not LiteralExpressionSyntax literalExpression)
            {
                return;
            }

            DiagnosticDescriptor? rule = CheckJustificationText(literalExpression.Token.ValueText);
            if (rule is not null)
            {
                literalExpression.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: rule
                );
            }
        }

        private static DiagnosticDescriptor? CheckJustificationText(string justificationText)
        {
            if (string.IsNullOrWhiteSpace(justificationText))
            {
                return RuleMustHaveJustification;
            }

            if (justificationText.StartsWith(value: "TODO", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                return RuleMustNotHaveTodoJustification;
            }

            return null;
        }

        private static AttributeArgumentSyntax? FindJustificationAttributeArgument(AttributeSyntax attributeSyntax)
        {
            return attributeSyntax.ArgumentList?.Arguments
                                  .FirstOrDefault(arg => StringComparer.Ordinal.Equals(
                                                      x: arg.NameEquals?.Name.Identifier.Text,
                                                      y: "Justification"
                                                  ));
        }
    }
}