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
    ///     Looks for issues with argument exception creation
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ArgumentExceptionAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Exceptions";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleMustPassParameterNameToArgumentExceptions,
                                                                                   category: CATEGORY,
                                                                                   title: "Argument Exceptions should pass parameter name",
                                                                                   message: "Argument Exceptions should pass parameter name");

        private static readonly string[] ArgumentExceptions = {"System.ArgumentException", "System.ArgumentNullException", "System.ArgumentOutOfRangeException"};

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: ArgumentExceptionsMustPassParameterName, SyntaxKind.ObjectCreationExpression);
        }

        private static void ArgumentExceptionsMustPassParameterName(SyntaxNodeAnalysisContext syntaxNodeContext)
        {
            if (syntaxNodeContext.Node is ObjectCreationExpressionSyntax objectCreation)
            {
                SymbolInfo symbolInfo = syntaxNodeContext.SemanticModel.GetSymbolInfo(objectCreation);

                if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
                {
                    if (IsArgumentException(methodSymbol.ReceiverType))
                    {
                        IParameterSymbol? parameterNameParameter = methodSymbol.Parameters.FirstOrDefault(parameter => parameter.Name == "paramName");

                        if (parameterNameParameter == null)
                        {
                            syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, objectCreation.GetLocation()));
                        }
                    }
                }
            }
        }

        private static bool IsArgumentException(ITypeSymbol? methodSymbolReceiverType)
        {
            if (methodSymbolReceiverType == null)
            {
                return false;
            }

            string typeName = methodSymbolReceiverType.ToString();

            return ArgumentExceptions.Contains(typeName);
        }
    }
}