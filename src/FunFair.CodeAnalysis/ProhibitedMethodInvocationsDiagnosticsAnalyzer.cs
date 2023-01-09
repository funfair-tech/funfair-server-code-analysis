using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <inheritdoc />
/// <summary>
///     Looks for prohibited methods with specific parameter invocation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ProhibitedMethodsSpec[] BannedMethods =
    {
        new(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage,
            title: @"Avoid use of assert method without message",
            message: "Only use Assert.True with message parameter",
            sourceClass: "Xunit.Assert",
            bannedMethod: "True",
            new[]
            {
                new[]
                {
                    "bool"
                }
            }),
        new(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage,
            title: @"Avoid use of assert method without message",
            message: "Only use Assert.False with message parameter",
            sourceClass: "Xunit.Assert",
            bannedMethod: "False",
            new[]
            {
                new[]
                {
                    "bool"
                }
            }),
        new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary,
            title: @"Avoid use of the built in AddOrUpdate methods",
            message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
            sourceClass: "NonBlocking.ConcurrentDictionary`2",
            bannedMethod: "AddOrUpdate",
            new[]
            {
                new[]
                {
                    "TKey",
                    "System.Func<TKey, TValue>",
                    "System.Func<TKey, TValue, TValue>"
                }
            }),
        new(ruleId: Rules.RuleDontUseBuildInAddOrUpdateConcurrentDictionary,
            title: @"Avoid use of the built in AddOrUpdate methods",
            message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
            sourceClass: "NonBlocking.ConcurrentDictionary`2",
            bannedMethod: "AddOrUpdate",
            new[]
            {
                new[]
                {
                    "TKey",
                    "TValue",
                    "System.Func<TKey, TValue, TValue>"
                }
            }),
        new(ruleId: Rules.RuleDontUseBuildInGetOrAddConcurrentDictionary,
            title: @"Avoid use of the built in GetOrAdd methods",
            message: "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
            sourceClass: "NonBlocking.ConcurrentDictionary`2",
            bannedMethod: "GetOrAdd",
            new[]
            {
                new[]
                {
                    "TKey",
                    "System.Func<TKey, TValue>"
                }
            })
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

        void LookForBannedInvocation(InvocationExpressionSyntax invocation, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
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
                    invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: prohibitedMethod.Rule);
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
        return sourceClassType.GetMembers()
                              .Where(predicate: x => x.Name == rule.BannedMethod)
                              .OfType<IMethodSymbol>()
                              .ToArray();
    }

    /// <summary>
    ///     Filter method signatures to get only signatures banned by rules
    /// </summary>
    /// <param name="methodSignatures">All signatures of one method</param>
    /// <param name="ruleSignatures">All banned signatures</param>
    /// <returns>Collection of allowed signatures.</returns>
    private static IReadOnlyList<IMethodSymbol> RemoveAllowedSignaturesForMethod(IReadOnlyList<IMethodSymbol>? methodSignatures, IEnumerable<IEnumerable<string>>? ruleSignatures)
    {
        if (methodSignatures == null)
        {
            return ThrowUnknownMethodSignatureException(methodSignatures);
        }

        if (ruleSignatures == null)
        {
            return ThrowUnknownRuleSignature(ruleSignatures);
        }

        List<IMethodSymbol> methodSymbols = methodSignatures.ToList();

        return BuildMethodSignatureList(ruleSignatures: ruleSignatures, methodSymbols: methodSymbols);
    }

    private static IReadOnlyList<IMethodSymbol> BuildMethodSignatureList(IEnumerable<IEnumerable<string>> ruleSignatures, IReadOnlyList<IMethodSymbol> methodSymbols)
    {
        return ruleSignatures.SelectMany(ruleSignature => GetBannedMethodSymbols(methodSymbols: methodSymbols, ruleSignature: ruleSignature))
                             .ToArray();
    }

    private static IEnumerable<IMethodSymbol> GetBannedMethodSymbols(IReadOnlyList<IMethodSymbol> methodSymbols, IEnumerable<string> ruleSignature)
    {
        return methodSymbols.Where(methodSymbol => methodSymbol.Parameters.Select(selector: parameterSymbol => SymbolDisplay.ToDisplayString(parameterSymbol.Type))
                                                               .SequenceEqual(second: ruleSignature, comparer: StringComparer.Ordinal));
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
                                                                                 .SequenceEqual(second: invocationParameters, comparer: StringComparer.Ordinal));
    }

    private sealed class ProhibitedMethodsSpec
    {
        public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod, IEnumerable<IEnumerable<string>> bannedSignatures)
        {
            this.SourceClass = sourceClass;
            this.BannedMethod = bannedMethod;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.IllegalMethodInvocations, title: title, message: message);
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