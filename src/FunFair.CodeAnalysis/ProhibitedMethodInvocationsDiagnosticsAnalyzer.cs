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
public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ProhibitedMethodsSpec[] BannedMethods =
    [
        Build(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage,
              title: "Avoid use of assert method without message",
              message: "Only use Assert.True with message parameter",
              sourceClass: "Xunit.Assert",
              bannedMethod: "True",
              [
                  [
                      "bool"
                  ]
              ]),
        Build(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage,
              title: "Avoid use of assert method without message",
              message: "Only use Assert.False with message parameter",
              sourceClass: "Xunit.Assert",
              bannedMethod: "False",
              [
                  [
                      "bool"
                  ]
              ]),
        Build(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary,
              title: "Avoid use of the built in AddOrUpdate methods",
              message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
              sourceClass: "NonBlocking.ConcurrentDictionary`2",
              bannedMethod: "AddOrUpdate",
              [
                  [
                      "TKey",
                      "System.Func<TKey, TValue>",
                      "System.Func<TKey, TValue, TValue>"
                  ]
              ]),
        Build(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary,
              title: "Avoid use of the built in AddOrUpdate methods",
              message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
              sourceClass: "NonBlocking.ConcurrentDictionary`2",
              bannedMethod: "AddOrUpdate",
              [
                  [
                      "TKey",
                      "TValue",
                      "System.Func<TKey, TValue, TValue>"
                  ]
              ]),
        Build(ruleId: Rules.RuleDontUseBuildInGetOrAddConcurrentDictionary,
              title: "Avoid use of the built in GetOrAdd methods",
              message: "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
              sourceClass: "NonBlocking.ConcurrentDictionary`2",
              bannedMethod: "GetOrAdd",
              [
                  [
                      "TKey",
                      "System.Func<TKey, TValue>"
                  ]
              ])
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        BannedMethods.Select(selector: r => r.Rule)
                     .ToImmutableArray();

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();

        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private static ProhibitedMethodsSpec Build(string ruleId,
                                               string title,
                                               string message,
                                               string sourceClass,
                                               string bannedMethod,
                                               IReadOnlyList<IReadOnlyList<string>> bannedSignatures)
    {
        return new(ruleId: ruleId, title: title, message: message, sourceClass: sourceClass, bannedMethod: bannedMethod, bannedSignatures: bannedSignatures);
    }

    private sealed class Checker
    {
        private IReadOnlyDictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>>? _cachedSymbols;

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: syntaxNodeAnalysisContext =>
                                                                         this.LookForBannedMethods(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                                                                   compilation: compilationStartContext.Compilation),
                                                             SyntaxKind.InvocationExpression);
        }

        private IReadOnlyDictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>> LoadCachedSymbols(Compilation compilation)
        {
            return this._cachedSymbols ??= BuildCachedSymbols(compilation);
        }

        private void LookForBannedInvocation(InvocationExpressionSyntax invocation, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Compilation compilation)
        {
            IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

            // check if there is at least one rule that correspond to invocation method
            if (memberSymbol is null)
            {
                return;
            }

            string? fullyQualifiedName = memberSymbol.ContainingType.ToFullyQualifiedName();

            if (fullyQualifiedName is null)
            {
                return;
            }

            IReadOnlyDictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>> cachedSymbols = this.LoadCachedSymbols(compilation);

            Mapping mapping = new(methodName: memberSymbol.Name, className: fullyQualifiedName);

            foreach (ProhibitedMethodsSpec prohibitedMethod in BannedMethods.Where(
                         predicate: rule => StringComparer.Ordinal.Equals(x: rule.QualifiedName, y: mapping.QualifiedName)))
            {
                if (!cachedSymbols.TryGetValue(key: prohibitedMethod, out IReadOnlyList<IMethodSymbol> prohibitedMethodSignatures))
                {
                    continue;
                }

                if (IsBannedMethodSignature(invocationArguments: memberSymbol, methodSignatures: prohibitedMethodSignatures))
                {
                    invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: prohibitedMethod.Rule);
                }
            }
        }

        private void LookForBannedMethods(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Compilation compilation)
        {
            if (syntaxNodeAnalysisContext.Node is InvocationExpressionSyntax invocation)
            {
                this.LookForBannedInvocation(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, compilation: compilation);
            }
        }

        private static IReadOnlyDictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>> BuildCachedSymbols(Compilation compilation)
        {
            Dictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>> cachedSymbols = new();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (cachedSymbols.ContainsKey(rule))
                {
                    continue;
                }

                INamedTypeSymbol? sourceClassType = compilation.GetTypeByMetadataName(rule.SourceClass);

                if (sourceClassType?.GetMembers()
                                   .IsEmpty != false)
                {
                    continue;
                }

                // get all method overloads
                IReadOnlyList<IMethodSymbol> methodSignatures = GetOverloads(sourceClassType: sourceClassType, rule: rule);

                if (methodSignatures.Count != 0)
                {
                    cachedSymbols.Add(key: rule, RemoveAllowedSignaturesForMethod(methodSignatures: methodSignatures, ruleSignatures: rule.BannedSignatures));
                }
            }

            return cachedSymbols;
        }

        private static IReadOnlyList<IMethodSymbol> GetOverloads(INamedTypeSymbol sourceClassType, ProhibitedMethodsSpec rule)
        {
            return
            [
                ..sourceClassType.GetMembers()
                                 .Where(predicate: x => StringComparer.Ordinal.Equals(x: x.Name, y: rule.BannedMethod))
                                 .OfType<IMethodSymbol>()
            ];
        }

        private static IReadOnlyList<IMethodSymbol> RemoveAllowedSignaturesForMethod(IReadOnlyList<IMethodSymbol>? methodSignatures,
                                                                                     IEnumerable<IEnumerable<string>>? ruleSignatures)
        {
            if (methodSignatures is null)
            {
                return ThrowUnknownMethodSignatureException(methodSignatures);
            }

            if (ruleSignatures is null)
            {
                return ThrowUnknownRuleSignature(ruleSignatures);
            }

            return BuildMethodSignatureList(ruleSignatures: ruleSignatures, methodSymbols: methodSignatures);
        }

        private static IReadOnlyList<IMethodSymbol> BuildMethodSignatureList(IEnumerable<IEnumerable<string>> ruleSignatures, IReadOnlyList<IMethodSymbol> methodSymbols)
        {
            return ruleSignatures.SelectMany(ruleSignature => methodSymbols.Where(methodSymbol => methodSymbol
                                                                                                  .Parameters.Select(
                                                                                                      selector: parameterSymbol =>
                                                                                                                    SymbolDisplay.ToDisplayString(parameterSymbol.Type))
                                                                                                  .SequenceEqual(second: ruleSignature, comparer: StringComparer.Ordinal)))
                                 .ToArray();
        }

        [SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S1172: Parameter only used for name", Justification = "By Design")]
        [SuppressMessage(category: "ReSharper", checkId: "EntityNameCapturedOnly.Local", Justification = "By Design")]
        private static IReadOnlyList<IMethodSymbol> ThrowUnknownRuleSignature(IEnumerable<IEnumerable<string>>? ruleSignatures)
        {
            throw new ArgumentException(message: "Unknown rule signature", nameof(ruleSignatures));
        }

        [SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S1172: Parameter only used for name", Justification = "By Design")]
        [SuppressMessage(category: "ReSharper", checkId: "EntityNameCapturedOnly.Local", Justification = "By Design")]
        private static IReadOnlyList<IMethodSymbol> ThrowUnknownMethodSignatureException(IEnumerable<IMethodSymbol>? methodSignatures)
        {
            throw new ArgumentException(message: "Unknown method signature", nameof(methodSignatures));
        }

        private static bool IsBannedMethodSignature(IMethodSymbol invocationArguments, IEnumerable<IMethodSymbol> methodSignatures)
        {
            IReadOnlyList<string> invocationParameters = GetInvocationParameters(invocationArguments);

            return methodSignatures.Any(predicate: methodSignature => methodSignature.Parameters.Select(x => SymbolDisplay.ToDisplayString(x.OriginalDefinition))
                                                                                     .SequenceEqual(second: invocationParameters, comparer: StringComparer.Ordinal));
        }

        private static IReadOnlyList<string> GetInvocationParameters(IMethodSymbol invocationArguments)
        {
            return invocationArguments.Parameters.Select(parameter => SymbolDisplay.ToDisplayString(parameter.OriginalDefinition))
                                      .ToArray();
        }
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {SourceClass} Banned Method: {BannedMethod}")]
    private readonly record struct ProhibitedMethodsSpec
    {
        public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod, IReadOnlyList<IReadOnlyList<string>> bannedSignatures)
        {
            this.SourceClass = sourceClass;
            this.BannedMethod = bannedMethod;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.IllegalMethodInvocations, title: title, message: message);
            this.BannedSignatures = bannedSignatures;
        }

        public string SourceClass { get; }

        public string BannedMethod { get; }

        public IReadOnlyList<IReadOnlyList<string>> BannedSignatures { get; }

        public DiagnosticDescriptor Rule { get; }

        public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.BannedMethod);
    }
}