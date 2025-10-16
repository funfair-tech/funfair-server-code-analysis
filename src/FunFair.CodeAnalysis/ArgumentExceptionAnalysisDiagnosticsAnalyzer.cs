using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArgumentExceptionAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private const string CATEGORY = Categories.Exceptions;

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleMustPassParameterNameToArgumentExceptions,
        category: CATEGORY,
        title: "Argument Exceptions should pass parameter name",
        message: "Argument Exceptions should pass parameter name"
    );

    private static readonly ImmutableHashSet<string> ArgumentExceptions = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "System.ArgumentException",
        "System.ArgumentNullException",
        "System.ArgumentOutOfRangeException"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

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
            action: checker.ArgumentExceptionsMustPassParameterName,
            SyntaxKind.ObjectCreationExpression
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<ITypeSymbol, bool> _typeCache = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<IMethodSymbol, bool> _methodCache = new(SymbolEqualityComparer.Default);

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void ArgumentExceptionsMustPassParameterName(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not ObjectCreationExpressionSyntax objectCreation)
            {
                return;
            }

            SymbolInfo symbolInfo = syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(
                expression: objectCreation,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

            if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
            {
                return;
            }

            if (!this.IsArgumentException(methodSymbol.ReceiverType))
            {
                return;
            }

            if (this.HasParamNameParameter(methodSymbol))
            {
                return;
            }

            syntaxNodeAnalysisContext.ReportDiagnostic(
                Diagnostic.Create(descriptor: Rule, objectCreation.GetLocation())
            );
        }

        private bool HasParamNameParameter(IMethodSymbol methodSymbol)
        {
            // Check cache first
            if (this._methodCache.TryGetValue(key: methodSymbol, out bool hasParam))
            {
                return hasParam;
            }

            // Compute and cache result
            bool result = methodSymbol.Parameters.Any(parameter =>
                StringComparer.Ordinal.Equals(x: parameter.Name, y: "paramName")
            );

            this._methodCache[methodSymbol] = result;
            return result;
        }

        private bool IsArgumentException(ITypeSymbol? methodSymbolReceiverType)
        {
            if (methodSymbolReceiverType is null)
            {
                return false;
            }

            // Check cache first
            if (this._typeCache.TryGetValue(key: methodSymbolReceiverType, out bool isArgumentEx))
            {
                return isArgumentEx;
            }

            // Compute and cache result
            string typeName = SymbolDisplay.ToDisplayString(methodSymbolReceiverType);
            bool result = ArgumentExceptions.Contains(typeName);

            this._typeCache[methodSymbolReceiverType] = result;
            return result;
        }
    }
}