using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ForceMethodParametersInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ForcedMethodsSpec[] ForcedMethods =
    [
        Build(
            ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
            title: "Avoid use of serializer without own JsonSerializerOptions parameter",
            message: "Only use JsonSerializer.Serialize with own JsonSerializerOptions",
            sourceClass: "System.Text.Json.JsonSerializer",
            forcedMethod: "Serialize",
            requiredArgumentCount: 2
        ),
        Build(
            ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
            title: "Avoid use of serializer without own JsonSerializerOptions parameter",
            message: "Only use JsonSerializer.Serialize with own JsonSerializerOptions",
            sourceClass: "System.Text.Json.JsonSerializer",
            forcedMethod: "SerializeAsync",
            requiredArgumentCount: 2
        ),
        Build(
            ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
            title: "Avoid use of deserializer without own JsonSerializerOptions parameter",
            message: "Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
            sourceClass: "System.Text.Json.JsonSerializer",
            forcedMethod: "Deserialize",
            requiredArgumentCount: 2
        ),
        Build(
            ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
            title: "Avoid use of deserializer without own JsonSerializerOptions parameter",
            message: "Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
            sourceClass: "System.Text.Json.JsonSerializer",
            forcedMethod: "DeserializeAsync",
            requiredArgumentCount: 2
        ),
        Build(
            ruleId: Rules.RuleDontUseSubstituteReceivedWithoutAmountOfCalls,
            title: "Avoid use of received without call count",
            message: "Only use Received with expected call count",
            sourceClass: "NSubstitute.SubstituteExtensions",
            forcedMethod: "Received",
            requiredArgumentCount: 1
        ),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [.. ForcedMethods.Select(selector: r => r.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: LookForForcedMethods, SyntaxKind.InvocationExpression);
    }

    private static void LookForForcedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax invocation)
        {
            return;
        }

        IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

        // check if there is at least one rule that correspond to invocation method
        if (memberSymbol is null)
        {
            return;
        }

        Mapping mapping = new(methodName: memberSymbol.Name, SymbolDisplay.ToDisplayString(memberSymbol.ContainingType));

        IEnumerable<ForcedMethodsSpec> forcedMethods = ForcedMethods.Where(predicate: rule => StringComparer.Ordinal.Equals(x: rule.QualifiedName, y: mapping.QualifiedName));

        foreach (ForcedMethodsSpec prohibitedMethod in forcedMethods)
        {
            if (!IsInvocationAllowed(invocationArguments: memberSymbol, argumentsInvokedCount: invocation.ArgumentList.Arguments.Count, requiredArgumentsCount: prohibitedMethod.RequiredArgumentCount))
            {
                invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: prohibitedMethod.Rule);
            }
        }
    }

    private static bool IsInvocationAllowed(IMethodSymbol invocationArguments, int argumentsInvokedCount, int requiredArgumentsCount)
    {
        bool allowedBasedOnArgumentTypeAndSequence = invocationArguments.Parameters.SequenceEqual(invocationArguments.Parameters);
        bool allowedBasedOnArgumentCount = argumentsInvokedCount >= requiredArgumentsCount;

        return allowedBasedOnArgumentCount && allowedBasedOnArgumentTypeAndSequence;
    }

    private static ForcedMethodsSpec Build(string ruleId, string title, string message, string sourceClass, string forcedMethod, int requiredArgumentCount)
    {
        return new(ruleId: ruleId, title: title, message: message, sourceClass: sourceClass, forcedMethod: forcedMethod, requiredArgumentCount: requiredArgumentCount);
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Prohibits {SourceClass}.{ForcedMethod}")]
    private readonly record struct ForcedMethodsSpec
    {
        public ForcedMethodsSpec(string ruleId, string title, string message, string sourceClass, string forcedMethod, int requiredArgumentCount)
        {
            this.SourceClass = sourceClass;
            this.ForcedMethod = forcedMethod;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.ForcedMethodInvocations, title: title, message: message);
            this.RequiredArgumentCount = requiredArgumentCount;
        }

        public string SourceClass { get; }

        public string ForcedMethod { get; }

        public int RequiredArgumentCount { get; }

        public DiagnosticDescriptor Rule { get; }

        public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);
    }
}
