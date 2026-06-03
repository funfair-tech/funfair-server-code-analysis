using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class InternalsVisibleToDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleDontUseInternalsVisibleTo,
        category: Categories.IllegalAttributes,
        title: "Do not use InternalsVisibleTo",
        message: "Do not use InternalsVisibleTo"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

    [SuppressMessage(
        category: "Nullable.Extended.Analyzer",
        checkId: "NX0001: Suppression of NullForgiving operator is not required",
        Justification = "Required here"
    )]
    private static readonly string InternalsVisibleToFullName = typeof(InternalsVisibleToAttribute).FullName!;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();
        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private sealed class Checker
    {
        private INamedTypeSymbol? _internalsVisibleTo;

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol? sourceClassType = this.GetInternalsVisibleToAttributeType(
                compilationStartContext.Compilation
            );

            if (sourceClassType is null)
            {
                return;
            }

            compilationStartContext.RegisterSyntaxNodeAction(
                action: syntaxNodeAnalysisContext =>
                    CheckInternalsVisibleToAttribute(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        sourceClassType: sourceClassType
                    ),
                SyntaxKind.Attribute
            );
        }

        private INamedTypeSymbol? GetInternalsVisibleToAttributeType(Compilation compilation)
        {
            return this._internalsVisibleTo ??= compilation.GetTypeByMetadataName(InternalsVisibleToFullName);
        }

        private static void CheckInternalsVisibleToAttribute(
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

            attributeSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
        }
    }
}
