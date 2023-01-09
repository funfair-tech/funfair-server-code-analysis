﻿using System;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for issues with argument exception creation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ArgumentExceptionAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private const string CATEGORY = Categories.Exceptions;

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleMustPassParameterNameToArgumentExceptions,
                                                                               category: CATEGORY,
                                                                               title: "Argument Exceptions should pass parameter name",
                                                                               message: "Argument Exceptions should pass parameter name");

    private static readonly string[] ArgumentExceptions =
    {
        "System.ArgumentException",
        "System.ArgumentNullException",
        "System.ArgumentOutOfRangeException"
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        new[]
        {
            Rule
        }.ToImmutableArray();

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
        if (syntaxNodeContext.Node is not ObjectCreationExpressionSyntax objectCreation)
        {
            return;
        }

        SymbolInfo symbolInfo = syntaxNodeContext.SemanticModel.GetSymbolInfo(objectCreation);

        if (symbolInfo.Symbol is not IMethodSymbol methodSymbol)
        {
            return;
        }

        if (!IsArgumentException(methodSymbol.ReceiverType))
        {
            return;
        }

        if (HasParamNameParameter(methodSymbol))
        {
            return;
        }

        ReportDiagnostics(syntaxNodeContext: syntaxNodeContext, objectCreation: objectCreation);
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeContext, ObjectCreationExpressionSyntax objectCreation)
    {
        syntaxNodeContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, objectCreation.GetLocation()));
    }

    private static bool HasParamNameParameter(IMethodSymbol methodSymbol)
    {
        return methodSymbol.Parameters.Any(parameter => parameter.Name == "paramName");
    }

    private static bool IsArgumentException(ITypeSymbol? methodSymbolReceiverType)
    {
        if (methodSymbolReceiverType == null)
        {
            return false;
        }

        string typeName = SymbolDisplay.ToDisplayString(methodSymbolReceiverType);

        return ArgumentExceptions.Contains(value: typeName, comparer: StringComparer.Ordinal);
    }
}