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
            new(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage, title: @"Avoid use of assert method without message", message: "Only use Assert.True with message parameter", sourceClass:
                "Xunit.Assert", bannedMethod: "True", new[] {new[] {new ParameterSpecs("bool")}}),
            new(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage, title: @"Avoid use of assert method without message", message: "Only use Assert.False with message parameter", sourceClass:
                "Xunit.Assert", bannedMethod: "False", new[] {new[] {new ParameterSpecs("bool")}}),
            new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary, title: @"Avoid use of the built in AddOrUpdate methods", message:
                "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "AddOrUpdate",
                new[]
                {
                    new[]
                    {
                        new ParameterSpecs("TKey"),
                        new ParameterSpecs(type: "System.Func<TKey, TValue>", allowLambdaExpressions: false),
                        new ParameterSpecs(type: "System.Func<TKey, TValue, TValue>", allowLambdaExpressions: false)
                    }
                }),
            new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary, title: @"Avoid use of the built in AddOrUpdate methods", message:
                "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "AddOrUpdate",
                new[] {new[] {new ParameterSpecs("TKey"), new ParameterSpecs(type: "TValue"), new ParameterSpecs(type: "System.Func<TKey, TValue, TValue>", allowLambdaExpressions: false)}}),
            new(ruleId: Rules.RuleDontUseBuildInGetOrAddConcurrentDictionary, title: @"Avoid use of the built in GetOrAdd methods", message:
                "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "GetOrAdd", new[] {new[] {new ParameterSpecs("TKey"), new ParameterSpecs(type: "TValue")}}),
            new(ruleId: Rules.RuleDontUseBuildInGetOrAddConcurrentDictionary, title: @"Avoid use of the built in GetOrAdd methods", message:
                "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used", sourceClass:
                "NonBlocking.ConcurrentDictionary`2", bannedMethod: "GetOrAdd", new[]
                                                                                {
                                                                                    new[]
                                                                                    {
                                                                                        new ParameterSpecs("TKey"),
                                                                                        new ParameterSpecs(type: "System.Func<TKey, TValue>", allowLambdaExpressions: false)
                                                                                    }
                                                                                }),
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

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                InvocationExpressionSyntax[] invocations = syntaxNodeAnalysisContext.Node.DescendantNodesAndSelf()
                                                                                    .OfType<InvocationExpressionSyntax>()
                                                                                    .ToArray();

                foreach (InvocationExpressionSyntax invocation in invocations)
                {
                    IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                    if (memberSymbol == null)
                    {
                        continue;
                    }

                    IReadOnlyList<InvocationArgument> invocationArguments = GetInvocationArguments(invocationExpressionSyntax: invocation, methodSymbol: memberSymbol)
                        .ToList();

                    if (invocationArguments.Count == 0)
                    {
                        continue;
                    }

                    string? fullyQualifiedName = memberSymbol.ContainingType.ToFullyQualifiedName();

                    if (fullyQualifiedName == null)
                    {
                        continue;
                    }

                    Mapping mapping = new(className: fullyQualifiedName, methodName: memberSymbol.Name);

                    foreach (ProhibitedMethodsSpec prohibitedMethodSpecs in BannedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName))
                    {
                        if (!cachedSymbols.TryGetValue(key: prohibitedMethodSpecs, out IReadOnlyList<IMethodSymbol> symbolsForProhibitedMethod))
                        {
                            continue;
                        }

                        if (IsBannedMethodSignature(invocationArguments: invocationArguments, prohibitedMethodsSpecs: prohibitedMethodSpecs, prohibitedMethodsSymbols: symbolsForProhibitedMethod))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: prohibitedMethodSpecs.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.MethodDeclaration);
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

        private static IEnumerable<bool> CheckIfInvocationArgumentUseLambdaExpression(InvocationExpressionSyntax invocation)
        {
            return invocation.ArgumentList.Arguments.Select(arg => arg.Expression is SimpleLambdaExpressionSyntax or ParenthesizedLambdaExpressionSyntax);
        }

        private static IEnumerable<InvocationArgument> GetInvocationArguments(InvocationExpressionSyntax invocationExpressionSyntax, IMethodSymbol methodSymbol)
        {
            bool[] lambdaParameters = CheckIfInvocationArgumentUseLambdaExpression(invocationExpressionSyntax)
                .ToArray();

            IList<InvocationArgument> invocationArguments = new List<InvocationArgument>();

            if (lambdaParameters.Length != methodSymbol.Parameters.Length)
            {
                return invocationArguments;
            }

            for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            {
                invocationArguments.Add(new InvocationArgument(parameterSymbol: methodSymbol.Parameters[i]
                                                                                            .OriginalDefinition,
                                                               lambdaParameters[i]));
            }

            return invocationArguments;
        }

        /// <summary>
        ///     Filter method signatures to get only signatures banned by rules
        /// </summary>
        /// <param name="methodSignatures">All signatures of one method</param>
        /// <param name="ruleSignatures">All banned signatures</param>
        /// <returns>Collection of allowed signatures.</returns>
        private static IReadOnlyList<IMethodSymbol> RemoveAllowedSignaturesForMethod(IEnumerable<IMethodSymbol> methodSignatures, IEnumerable<IEnumerable<ParameterSpecs>> ruleSignatures)
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
            foreach (IEnumerable<ParameterSpecs> ruleSignature in ruleSignatures)
            {
                IEnumerable<string> ruleSignatureTypes = ruleSignature.Select(signature => signature.Type);

                IEnumerable<IMethodSymbol> bannedMethodSymbols = methodSymbols.Where(methodSymbol => methodSymbol
                                                                                                     .Parameters
                                                                                                     .Select(selector: parameterSymbol => SymbolDisplay.ToDisplayString(parameterSymbol.Type))
                                                                                                     .SequenceEqual(ruleSignatureTypes));

                methodSignatureList.AddRange(bannedMethodSymbols);
            }

            return methodSignatureList;
        }

        private static bool IsBannedMethodSignature(IReadOnlyList<InvocationArgument> invocationArguments,
                                                    ProhibitedMethodsSpec prohibitedMethodsSpecs,
                                                    IReadOnlyList<IMethodSymbol> prohibitedMethodsSymbols)
        {
            // check if method is prohibited
            if (prohibitedMethodsSymbols.Any(symbol => symbol.Parameters.SequenceEqual(invocationArguments.Select(invocationArgument => invocationArgument.ParameterSymbol))))
            {
                return !LambdaCheckNeeded(invocationArguments: invocationArguments, prohibitedMethodsSpecs: prohibitedMethodsSpecs, out IReadOnlyList<ParameterSpecs> bannedSignature) ||
                       IncorrectUseOfLambdas(invocationArguments: invocationArguments, bannedSignature: bannedSignature);
            }

            return false;
        }

        private static bool IncorrectUseOfLambdas(IReadOnlyList<InvocationArgument> invocationArguments, IReadOnlyList<ParameterSpecs> bannedSignature)
        {
            for (int i = 0; i < bannedSignature.Count; i++)
            {
                if (!bannedSignature[i]
                    .AllowLambdaExpressions && invocationArguments[i]
                    .IsLambdaExpression)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LambdaCheckNeeded(IReadOnlyList<InvocationArgument> invocationArguments, ProhibitedMethodsSpec prohibitedMethodsSpecs, out IReadOnlyList<ParameterSpecs> bannedSignature)
        {
            // invocation is present in symbol and we can check if invocation is using lambdas according to defined rule
            // transform to parameters specs to match parameters between invocation and prohibited method
            IEnumerable<string> invocationParameterSpecs = invocationArguments.Select(argument => SymbolDisplay.ToDisplayString(argument.ParameterSymbol.Type));

            // find rule signature that we need
            bannedSignature = prohibitedMethodsSpecs.BannedSignatures.First(ruleSignature => ruleSignature.Select(parameterSpecs => parameterSpecs.Type)
                                                                                                          .SequenceEqual(invocationParameterSpecs))
                                                    .ToList();

            return !bannedSignature.All(parameter => parameter.AllowLambdaExpressions);
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod, IEnumerable<IEnumerable<ParameterSpecs>> bannedSignatures)
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
            public IEnumerable<IEnumerable<ParameterSpecs>> BannedSignatures { get; }

            public DiagnosticDescriptor Rule { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.BannedMethod);
        }

        private sealed class ParameterSpecs : IEquatable<ParameterSpecs>
        {
            public ParameterSpecs(string type, bool allowLambdaExpressions = true)
            {
                this.Type = type;
                this.AllowLambdaExpressions = allowLambdaExpressions;
            }

            public string Type { get; }

            public bool AllowLambdaExpressions { get; }

            /// <inheritdoc />
            public bool Equals(ParameterSpecs? other)
            {
                return AreEqual(this, right: other);
            }

            /// <inheritdoc />
            public override bool Equals(object? obj)
            {
                if (!(obj is ParameterSpecs other))
                {
                    return false;
                }

                return AreEqual(this, right: other);
            }

            /// <inheritdoc />
            public override int GetHashCode()
            {
                unchecked // Overflow is fine, just wrap
                {
                    int hash = 17;

                    hash = (hash * 23) + this.Type.GetHashCode();
                    hash = (hash * 23) + this.AllowLambdaExpressions.GetHashCode();

                    return hash;
                }
            }

            /// <summary>
            ///     Equality comparison via operator overload
            /// </summary>
            /// <param name="left">The first <see cref="ParameterSpecs" />.</param>
            /// <param name="right">The second <see cref="ParameterSpecs" />.</param>
            /// <returns>true, if they are the same; otherwise, false.</returns>
            public static bool operator ==(ParameterSpecs? left, ParameterSpecs? right)
            {
                return AreEqual(left: left, right: right);
            }

            /// <summary>
            ///     Inequality comparison via operator overload.
            /// </summary>
            /// <param name="left">The first <see cref="ParameterSpecs" />.</param>
            /// <param name="right">The second <see cref="ParameterSpecs" />.</param>
            /// <returns>true, if they are different; otherwise, false.</returns>
            public static bool operator !=(ParameterSpecs? left, ParameterSpecs? right)
            {
                return !AreEqual(left: left, right: right);
            }

            private static bool AreEqual(ParameterSpecs? left, ParameterSpecs? right)
            {
                static bool Equality(ParameterSpecs l, ParameterSpecs r)
                {
                    return l.Type == r.Type && l.AllowLambdaExpressions == r.AllowLambdaExpressions;
                }

                if (ReferenceEquals(objA: left, objB: right))
                {
                    return true;
                }

                if (ReferenceEquals(objA: null, objB: right))
                {
                    return false;
                }

                if (ReferenceEquals(objA: null, objB: left))
                {
                    return false;
                }

                return Equality(l: left, r: right);
            }
        }

        private sealed class InvocationArgument
        {
            public InvocationArgument(IParameterSymbol parameterSymbol, bool isLambdaExpression)
            {
                this.ParameterSymbol = parameterSymbol;
                this.IsLambdaExpression = isLambdaExpression;
            }

            public IParameterSymbol ParameterSymbol { get; }

            public bool IsLambdaExpression { get; }
        }
    }
}