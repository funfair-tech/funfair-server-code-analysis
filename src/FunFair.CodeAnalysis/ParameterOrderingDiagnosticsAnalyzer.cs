using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with parameter ordering
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParameterOrderingDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Parameters";

        private static readonly IReadOnlyList<string> PreferredEndingOrdering = new[]
                                                                                {
                                                                                    "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                                                                                    "Microsoft.Extensions.Logging.ILogger",
                                                                                    "System.Threading.CancellationToken"
                                                                                };

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleParametersShouldBeInOrder,
                                                                                   category: CATEGORY,
                                                                                   title: "Parameters are out of order",
                                                                                   message: "Parameter {0} must be the {1} parameter");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(this.PerformCheck);
        }

        private void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: this.MustBeInASaneOrder, SyntaxKind.ParameterList);
        }

        private void MustBeInASaneOrder(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is ParameterListSyntax parameterList)
            {
                var parameters = parameterList.Parameters.Select((parameter, index) => new
                                                                                       {
                                                                                           Parameter = parameter,
                                                                                           Index = index,
                                                                                           FullTypeName = ParameterHelpers.GetFullTypeName(
                                                                                               syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                                                               parameterSyntax: parameter)
                                                                                       })
                                              .ToArray();

                List<string> matchedEndings = new List<string>();

                foreach (var parameterType in PreferredEndingOrdering.Reverse())
                {
                    var matchingParameter = parameters.FirstOrDefault(x => x.FullTypeName == parameterType);

                    if (matchingParameter != null)
                    {
                        matchedEndings.Add(parameterType);

                        int parameterIndex = parameters.Length - matchingParameter.Index;
                        int requiredParameterIndex = parameters.Length - matchedEndings.Count;

                        if (parameterIndex != requiredParameterIndex)
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule,
                                                                                         matchingParameter.Parameter.GetLocation(),
                                                                                         matchingParameter.Parameter.Identifier.Text,
                                                                                         requiredParameterIndex));
                        }
                    }
                }
            }
        }
    }
}