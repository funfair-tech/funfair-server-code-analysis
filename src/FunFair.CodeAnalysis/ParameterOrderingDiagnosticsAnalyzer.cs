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
    private static readonly IReadOnlyList<string> PreferredEndingOrdering =
    [
        "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
        "Microsoft.Extensions.Logging.ILogger",
        "System.Threading.CancellationToken"
    ];

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleParametersShouldBeInOrder,
                                                                               category: Categories.Parameters,
                                                                               title: "Parameters are out of order",
                                                                               message: "Parameter '{0}' must be parameter {1}");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: MustBeInASaneOrder, SyntaxKind.ParameterList);
    }

    private static void MustBeInASaneOrder(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not ParameterListSyntax parameterList)
        {
            return;
        }

        ParameterItem[] parameters = BuildParameters(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterList: parameterList);

        List<string> matchedEndings = [];

        foreach (string parameterType in PreferredEndingOrdering.Reverse())
        {
            ParameterItem? match = FindParameter(parameters: parameters, parameterType: parameterType);

            if (match is null)
            {
                continue;
            }

            ParameterItem matchingParameter = match.Value;

            if (matchingParameter.Parameter.Modifiers.Any(pm => pm.IsKind(SyntaxKind.ThisKeyword)))
            {
                // Ignore parameters that are extension methods - they have to be the first parameter
                continue;
            }

            matchedEndings.Add(parameterType);

            int parameterIndex = matchingParameter.Index;
            int requiredParameterIndex = parameters.Length - matchedEndings.Count;

            if (parameterIndex != requiredParameterIndex)
            {
                matchingParameter.Parameter.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                              rule: Rule,
                                                              matchingParameter.Parameter.Identifier.Text,
                                                              requiredParameterIndex + 1);
            }
        }
    }

    [SuppressMessage(category: "Nullable.Extended.Analyzer", checkId: "NX0003: Suppression of NullForgiving operator is not required", Justification = "Required here")]
    private static ParameterItem[] BuildParameters(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ParameterListSyntax parameterList)
    {
        return
        [
            ..parameterList.Parameters.Select((parameter, index) => new ParameterItem(parameter: parameter,
                                                                                      index: index,
                                                                                      ParameterHelpers.GetFullTypeName(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                                                                                       parameterSyntax: parameter,
                                                                                                                       cancellationToken: syntaxNodeAnalysisContext
                                                                                                                           .CancellationToken)!))
        ];
    }

    [SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S3267: Use Linq", Justification = "Not here")]
    private static ParameterItem? FindParameter(IReadOnlyList<ParameterItem> parameters, string parameterType)
    {
        foreach (ParameterItem parameter in parameters)
        {
            if (StringComparer.Ordinal.Equals(x: parameter.FullTypeName, y: parameterType))
            {
                return parameter;
            }
        }

        return null;
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