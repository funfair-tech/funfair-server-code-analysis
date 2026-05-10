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

    private static readonly DiagnosticDescriptor RuleNotPermitted = RuleHelpers.CreateRule(
        code: Rules.RuleSuppressMessageNotPermitted,
        category: Categories.SuppressedErrors,
        title: "SuppressMessage is not permitted for this warning",
        message: "SuppressMessage is not permitted for '{0}'"
    );

    [SuppressMessage(
        category: "Nullable.Extended.Analyzer",
        checkId: "NX0001: Suppression of NullForgiving operator is not required",
        Justification = "Required here"
    )]
    private static readonly string SuppressMessageFullName = typeof(SuppressMessageAttribute).FullName!;

    private static readonly ImmutableArray<AllowedSuppression> AllowedSuppressions =
    [
        new(
            category: "Roslynator.Analyzers",
            checkIdPrefix: "RCS1231",
            whenAllowed: static context =>
            {
                if (context.Node is not AttributeSyntax attribute)
                {
                    return false;
                }

                SyntaxNode? parent = attribute.Parent?.Parent;

                if (parent is not MethodDeclarationSyntax method)
                {
                    return false;
                }

                return method.ParameterList.Parameters.Any(p =>
                    p.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)) && IsReadOnlySpanType(p.Type)
                );
            }
        ),
    ];

    private static bool IsReadOnlySpanType(TypeSyntax? type)
    {
        return type switch
        {
            GenericNameSyntax generic =>
                StringComparer.Ordinal.Equals(x: generic.Identifier.Text, y: "ReadOnlySpan"),
            QualifiedNameSyntax { Right: GenericNameSyntax rightGeneric } =>
                StringComparer.Ordinal.Equals(x: rightGeneric.Identifier.Text, y: "ReadOnlySpan"),
            _ => false,
        };
    }

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        SupportedDiagnosisList.Build(RuleMustHaveJustification, RuleMustNotHaveTodoJustification, RuleNotPermitted);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();
        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private sealed class AllowedSuppression
    {
        public AllowedSuppression(
            string category,
            string checkIdPrefix,
            Func<SyntaxNodeAnalysisContext, bool> whenAllowed
        )
        {
            this.Category = category;
            this.CheckIdPrefix = checkIdPrefix;
            this.WhenAllowed = whenAllowed;
        }

        public string Category { get; }

        public string CheckIdPrefix { get; }

        public Func<SyntaxNodeAnalysisContext, bool> WhenAllowed { get; }
    }

    private sealed class Checker
    {
        private INamedTypeSymbol? _suppressMessage;

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol? sourceClassType = this.GetSuppressMessageAttributeType(
                compilationStartContext.Compilation
            );

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

            if (!IsPermittedSuppression(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, attributeSyntax: attributeSyntax))
            {
                string? checkId = GetStringAttributeArgument(
                    attributeSyntax: attributeSyntax,
                    argumentName: "checkId",
                    position: 1
                );

                attributeSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: RuleNotPermitted,
                    checkId ?? string.Empty
                );

                return;
            }

            AttributeArgumentSyntax? findJustificationAttributeArgument = FindJustificationAttributeArgument(
                attributeSyntax
            );

            if (findJustificationAttributeArgument is null)
            {
                attributeSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: RuleMustHaveJustification
                );

                return;
            }

            if (findJustificationAttributeArgument.Expression is not LiteralExpressionSyntax literalExpression)
            {
                return;
            }

            DiagnosticDescriptor? rule = CheckJustificationText(literalExpression.Token.ValueText);

            if (rule is not null)
            {
                literalExpression.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: rule);
            }
        }

        private static bool IsPermittedSuppression(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            AttributeSyntax attributeSyntax
        )
        {
            string? category = GetStringAttributeArgument(
                attributeSyntax: attributeSyntax,
                argumentName: "category",
                position: 0
            );
            string? checkId = GetStringAttributeArgument(
                attributeSyntax: attributeSyntax,
                argumentName: "checkId",
                position: 1
            );

            if (category is null || checkId is null)
            {
                return false;
            }

            SyntaxNodeAnalysisContext context = syntaxNodeAnalysisContext;

            return AllowedSuppressions.Any(entry =>
                StringComparer.Ordinal.Equals(x: entry.Category, y: category)
                && CheckIdMatchesPrefix(checkId: checkId, checkIdPrefix: entry.CheckIdPrefix)
                && entry.WhenAllowed(context)
            );
        }

        private static string? GetStringAttributeArgument(
            AttributeSyntax attributeSyntax,
            string argumentName,
            int position
        )
        {
            if (attributeSyntax.ArgumentList is null)
            {
                return null;
            }

            AttributeArgumentSyntax? named = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(a =>
                StringComparer.Ordinal.Equals(x: a.NameColon?.Name.Identifier.Text, y: argumentName)
            );

            if (named?.Expression is LiteralExpressionSyntax namedLit)
            {
                return namedLit.Token.ValueText;
            }

            AttributeArgumentSyntax? positional = attributeSyntax
                .ArgumentList.Arguments.Where(a => a.NameColon is null && a.NameEquals is null)
                .Skip(position)
                .FirstOrDefault();

            if (positional?.Expression is LiteralExpressionSyntax posLit)
            {
                return posLit.Token.ValueText;
            }

            return null;
        }

        private static bool CheckIdMatchesPrefix(string checkId, string checkIdPrefix)
        {
            if (!checkId.StartsWith(value: checkIdPrefix, comparisonType: StringComparison.Ordinal))
            {
                return false;
            }

            if (checkId.Length == checkIdPrefix.Length)
            {
                return true;
            }

            char next = checkId[checkIdPrefix.Length];

            return next is ':' or ' ';
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
            return attributeSyntax.ArgumentList?.Arguments.FirstOrDefault(arg =>
                StringComparer.Ordinal.Equals(x: arg.NameEquals?.Name.Identifier.Text, y: "Justification")
            );
        }
    }
}
