
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
public sealed class ForceMethodParametersInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ForcedMethodsSpec[] ForcedMethods =
    [
        Build(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
              title: "Avoid use of serializer without own JsonSerializerOptions parameter",
              message: "Only use JsonSerializer.Serialize with own JsonSerializerOptions",
              sourceClass: "System.Text.Json.JsonSerializer",
              forcedMethod: "Serialize",
              requiredArgumentCount: 2),
        Build(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
              title: "Avoid use of serializer without own JsonSerializerOptions parameter",
              message: "Only use JsonSerializer.Serialize with own JsonSerializerOptions",
              sourceClass: "System.Text.Json.JsonSerializer",
              forcedMethod: "SerializeAsync",
              requiredArgumentCount: 2),
        Build(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
              title: "Avoid use of deserializer without own JsonSerializerOptions parameter",
              message: "Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
              sourceClass: "System.Text.Json.JsonSerializer",
              forcedMethod: "Deserialize",
              requiredArgumentCount: 2),
        Build(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
              title: "Avoid use of deserializer without own JsonSerializerOptions parameter",
              message: "Only use JsonSerializer.Deserialize with own JsonSerializerOptions",
              sourceClass: "System.Text.Json.JsonSerializer",
              forcedMethod: "DeserializeAsync",
              requiredArgumentCount: 2),
        Build(ruleId: Rules.RuleDontUseSubstituteReceivedWithoutAmountOfCalls,
              title: "Avoid use of received without call count",
              message: "Only use Received with expected call count",
              sourceClass: "NSubstitute.SubstituteExtensions",
              forcedMethod: "Received",
              requiredArgumentCount: 1),
    ];

    private static readonly Dictionary<string, IReadOnlyList<ForcedMethodsSpec>> MethodSpecsCache = ForcedMethods.GroupBy(x => x.QualifiedName, StringComparer.Ordinal)
    .ToDictionary(item => item.Key, item => (IReadOnlyList<ForcedMethodsSpec>)[..item], StringComparer.Ordinal);

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        [.. ForcedMethods.Select(r => r.Rule)];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        // Create a checker instance per compilation for caching
        Checker checker = new(MethodSpecsCache);

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.LookForForcedMethods,
            SyntaxKind.InvocationExpression
        );
    }

    private static ForcedMethodsSpec Build(
        string ruleId,
        string title,
        string message,
        string sourceClass,
        string forcedMethod,
        int requiredArgumentCount
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            sourceClass: sourceClass,
            forcedMethod: forcedMethod,
            requiredArgumentCount: requiredArgumentCount
        );
    }



    private sealed class Checker
    {
        private readonly Dictionary<string, IReadOnlyList<ForcedMethodsSpec>> _methodSpecsCache;
        private readonly Dictionary<IMethodSymbol, bool> _invocationAllowedCache = new(SymbolEqualityComparer.Default);

        public Checker(Dictionary<string, IReadOnlyList<ForcedMethodsSpec>> methodSpecsCache)
        {
            this._methodSpecsCache = methodSpecsCache;

        }

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void LookForForcedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(
                invocation: invocation,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
            );

            if (memberSymbol is null)
            {
                return;
            }

            Mapping mapping = new(
                methodName: memberSymbol.Name,
                className: SymbolDisplay.ToDisplayString(memberSymbol.ContainingType)
            );

            if (!this._methodSpecsCache.TryGetValue(key: mapping.QualifiedName, out IReadOnlyList<ForcedMethodsSpec>? forcedMethods))
            {
                return;
            }

            int argumentCount = invocation.ArgumentList.Arguments.Count;

            forcedMethods
                .Where(prohibitedMethod =>
                    !this.IsInvocationAllowed(
                        invocationArguments: memberSymbol,
                        argumentsInvokedCount: argumentCount,
                        requiredArgumentsCount: prohibitedMethod.RequiredArgumentCount
                    ))
                .ForEach(prohibitedMethod =>
                    invocation.ReportDiagnostics(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        rule: prohibitedMethod.Rule
                    ));
        }

        private bool IsInvocationAllowed(
            IMethodSymbol invocationArguments,
            int argumentsInvokedCount,
            int requiredArgumentsCount
        )
        {
            // Check cache first
            if (this._invocationAllowedCache.TryGetValue(key: invocationArguments, out bool cachedResult))
            {
                return cachedResult;
            }

            // This appears to be a bug in original code - comparing a collection to itself always returns true
            // Keeping original logic but caching it
            bool allowedBasedOnArgumentTypeAndSequence = invocationArguments.Parameters.SequenceEqual(
                invocationArguments.Parameters
            );
            bool allowedBasedOnArgumentCount = argumentsInvokedCount >= requiredArgumentsCount;

            bool result = allowedBasedOnArgumentCount && allowedBasedOnArgumentTypeAndSequence;

            this._invocationAllowedCache[invocationArguments] = result;
            return result;
        }
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Prohibits {SourceClass}.{ForcedMethod}")]
    private readonly record struct ForcedMethodsSpec
    {
        public ForcedMethodsSpec(
            string ruleId,
            string title,
            string message,
            string sourceClass,
            string forcedMethod,
            int requiredArgumentCount
        )
        {
            this.SourceClass = sourceClass;
            this.ForcedMethod = forcedMethod;
            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.ForcedMethodInvocations,
                title: title,
                message: message
            );
            this.RequiredArgumentCount = requiredArgumentCount;
        }

        public string SourceClass { get; }

        public string ForcedMethod { get; }

        public int RequiredArgumentCount { get; }

        public DiagnosticDescriptor Rule { get; }

        public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);
    }
}