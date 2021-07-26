using System;
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
    /// <inheritdoc />
    /// <summary>
    ///     Looks for prohibited methods with specific parameter invocation
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Illegal Method Invocations";

        private static readonly ProhibitedMethodsSpec[] BannedMethods =
        {
            new(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage, title: @"Avoid use of assert method without message", message: "Only use Assert.True with message parameter",
                sourceClass: "Xunit.Assert", bannedMethod: "True", new[] {new[] {"bool"}}),
            new(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage, title: @"Avoid use of assert method without message", message:
                "Only use Assert.False with message parameter", sourceClass: "Xunit.Assert", bannedMethod: "False", new[] {new[] {"bool"}}),
            new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary, title: @"Avoid use of the built in AddOrUpdate methods", message:
                "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "AddOrUpdate", new[] {new[] {"TKey", "System.Func<TKey, TValue>", "System.Func<TKey, TValue, TValue>"}}),
            new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary, title: @"Avoid use of the built in AddOrUpdate methods", message:
                "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "AddOrUpdate", new[] {new[] {"TKey", "TValue", "System.Func<TKey, TValue, TValue>"}}),
            new(ruleId: Rules.RuleDontUseBuildInGetOrAddConcurrentDictionary, title: @"Avoid use of the built in GetOrAdd methods", message:
                "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "GetOrAdd", new[] {new[] {"TKey", "System.Func<TKey, TValue>"}})
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            BannedMethods.Select(selector: r => r.Rule)
                         .ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        /// <summary>
        ///     Perform check over code base
        /// </summary>
        /// <param name="compilationStartContext"></param>
        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            IReadOnlyDictionary<ProhibitedMethodsSpec, IReadOnlyList<IMethodSymbol>> cachedSymbols = BuildCachedSymbols(compilationStartContext.Compilation);

            void LookForBannedInvocation(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                // check if there is at least one rule that correspond to invocation method
                if (memberSymbol == null)
                {
                    return;
                }

                string? fullyQualifiedName = memberSymbol.ContainingType.ToFullyQualifiedName();

                if (fullyQualifiedName == null)
                {
                    return;
                }

                Mapping mapping = new(methodName: memberSymbol.Name, className: fullyQualifiedName);

                foreach (ProhibitedMethodsSpec prohibitedMethod in BannedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName))
                {
                    if (!cachedSymbols.TryGetValue(key: prohibitedMethod, out IReadOnlyList<IMethodSymbol> prohibitedMethodSignatures))
                    {
                        continue;
                    }

                    if (IsBannedMethodSignature(invocationArguments: memberSymbol, methodSignatures: prohibitedMethodSignatures))
                    {
                        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: prohibitedMethod.Rule, invocation.GetLocation()));
                    }
                }
            }

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                if (syntaxNodeAnalysisContext.Node is InvocationExpressionSyntax invocation)
                {
                    LookForBannedInvocation(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.InvocationExpression);
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

                if (sourceClassType == null || sourceClassType.GetMembers() == default)
                {
                    continue;
                }

                // get all method overloads
                IMethodSymbol[] methodSignatures = sourceClassType.GetMembers()
                                                                  .Where(predicate: x => x.Name == rule.BannedMethod)
                                                                  .OfType<IMethodSymbol>()
                                                                  .ToArray();

                if (methodSignatures.Length > 0)
                {
                    cachedSymbols.Add(key: rule, RemoveAllowedSignaturesForMethod(methodSignatures: methodSignatures, ruleSignatures: rule.BannedSignatures));
                }
            }

            return cachedSymbols;
        }

        /// <summary>
        ///     Filter method signatures to get only signatures banned by rules
        /// </summary>
        /// <param name="methodSignatures">All signatures of one method</param>
        /// <param name="ruleSignatures">All banned signatures</param>
        /// <returns>Collection of allowed signatures.</returns>
        private static IReadOnlyList<IMethodSymbol> RemoveAllowedSignaturesForMethod(IEnumerable<IMethodSymbol> methodSignatures, IEnumerable<IEnumerable<string>> ruleSignatures)
        {
            if (methodSignatures == null)
            {
                throw new ArgumentException(message: "Unknown method signature");
            }

            if (ruleSignatures == null)
            {
                throw new ArgumentException(message: "Unknown rule signature");
            }

            IEnumerable<IMethodSymbol> methodSymbols = methodSignatures.ToList();
            List<IMethodSymbol> methodSignatureList = new();

            // iterate over each rule to find symbol signature that correspond with rule it self
            foreach (IEnumerable<string> ruleSignature in ruleSignatures)
            {
                IEnumerable<IMethodSymbol> bannedMethodSymbols = methodSymbols.Where(methodSymbol => methodSymbol
                                                                                                     .Parameters.Select(
                                                                                                         selector: parameterSymbol =>
                                                                                                                       SymbolDisplay.ToDisplayString(parameterSymbol.Type))
                                                                                                     .SequenceEqual(ruleSignature));

                methodSignatureList.AddRange(bannedMethodSymbols);
            }

            return methodSignatureList;
        }

        /// <summary>
        ///     Check if invoked method signature is in list of allowed signatures
        /// </summary>
        /// <param name="invocationArguments">Arguments used in invocation of method</param>
        /// <param name="methodSignatures">List of all blocked signatures for method</param>
        /// <returns>true, if the method was allowed; otherwise, false.</returns>
        private static bool IsBannedMethodSignature(IMethodSymbol invocationArguments, IEnumerable<IMethodSymbol> methodSignatures)
        {
            IEnumerable<string> invocationParameters = invocationArguments.Parameters.Select(parameter => SymbolDisplay.ToDisplayString(parameter.OriginalDefinition));

            return methodSignatures.Any(predicate: methodSignature => methodSignature.Parameters.Select(x => SymbolDisplay.ToDisplayString(x.OriginalDefinition))
                                                                                     .SequenceEqual(invocationParameters));
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod, IEnumerable<IEnumerable<string>> bannedSignatures)
            {
                this.SourceClass = sourceClass;
                this.BannedMethod = bannedMethod;
                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
                this.BannedSignatures = bannedSignatures;
            }

            public string SourceClass { get; }

            public string BannedMethod { get; }

            /// <summary>
            ///     List of all method signatures that are banned, every signature is given with array of types in exact parameter order
            /// </summary>
            public IEnumerable<IEnumerable<string>> BannedSignatures { get; }

            public DiagnosticDescriptor Rule { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.BannedMethod);
        }
    }
}