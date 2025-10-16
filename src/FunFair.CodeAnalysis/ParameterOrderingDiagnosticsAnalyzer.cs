using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
public sealed class ParameterOrderingDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableArray<string> PreferredEndingOrdering = [
        "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
        "Microsoft.Extensions.Logging.ILogger",
        "System.Threading.CancellationToken"
    ];

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleParametersShouldBeInOrder,
        category: Categories.Parameters,
        title: "Parameters are out of order",
        message: "Parameter '{0}' must be parameter {1}"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

    private static readonly StringComparer TypeNameComparer = StringComparer.Ordinal;

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
            action: checker.MustBeInASaneOrder,
            SyntaxKind.ParameterList
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<ParameterSyntax, string?> _typeNameCache = [];

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void MustBeInASaneOrder(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not ParameterListSyntax parameterList)
            {
                return;
            }

            IReadOnlyList<ParameterItem> parameters = this.BuildParameters(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parameterList: parameterList
            );

            List<string> matchedEndings = [];

            PreferredEndingOrdering
                .Reverse()
                .ForEach(parameterType =>
                    ProcessParameterType(
                        parameterType: parameterType,
                        parameters: parameters,
                        matchedEndings: matchedEndings,
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
                    ));
        }

        private static void ProcessParameterType(
            string parameterType,
            IReadOnlyList<ParameterItem> parameters,
            List<string> matchedEndings,
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext
        )
        {
            ParameterItem? match = FindParameter(parameters: parameters, parameterType: parameterType);

            if (match is null)
            {
                return;
            }

            ParameterItem matchingParameter = match.Value;

            if (matchingParameter.Parameter.Modifiers.Any(pm => pm.IsKind(SyntaxKind.ThisKeyword)))
            {
                // Ignore parameters that are extension methods - they have to be the first parameter
                return;
            }

            matchedEndings.Add(parameterType);

            int parameterIndex = matchingParameter.Index;
            int requiredParameterIndex = parameters.Count - matchedEndings.Count;

            if (parameterIndex != requiredParameterIndex)
            {
                matchingParameter.Parameter.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: Rule,
                    matchingParameter.Parameter.Identifier.Text,
                    requiredParameterIndex + 1
                );
            }
        }

        [SuppressMessage(
            category: "Nullable.Extended.Analyzer",
            checkId: "NX0003: Suppression of NullForgiving operator is not required",
            Justification = "Required here"
        )]
        private IReadOnlyList<ParameterItem> BuildParameters(
            SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ParameterListSyntax parameterList
        )
        {
            return [..parameterList.Parameters
                .Select((parameter, index) =>
                    new ParameterItem(
                        parameter: parameter,
                        index: index,
                        this.GetFullTypeName(
                            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                            parameterSyntax: parameter
                        )!
                    ))
                ];
        }

        private string? GetFullTypeName(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ParameterSyntax parameterSyntax
        )
        {
            // Check cache first
            if (this._typeNameCache.TryGetValue(key: parameterSyntax, out string? cachedTypeName))
            {
                return cachedTypeName;
            }

            // Compute and cache result
            string? typeName = ParameterHelpers.GetFullTypeName(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parameterSyntax: parameterSyntax,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

            this._typeNameCache[parameterSyntax] = typeName;
            return typeName;
        }

        private static ParameterItem? FindParameter(IReadOnlyList<ParameterItem> parameters, string parameterType)
        {
            return parameters.FirstOrNull(parameter => TypeNameComparer.Equals(x: parameter.FullTypeName, y: parameterType));
        }
    }

    [DebuggerDisplay("{Parameter.Identifier.Text} {Index} {FullTypeName}")]
    private readonly record struct ParameterItem
    {
        public ParameterItem(ParameterSyntax parameter, int index, string fullTypeName)
        {
            this.Parameter = parameter;
            this.Index = index;
            this.FullTypeName = fullTypeName;
        }

        public ParameterSyntax Parameter { get; }

        public int Index { get; }

        public string FullTypeName { get; }
    }
}