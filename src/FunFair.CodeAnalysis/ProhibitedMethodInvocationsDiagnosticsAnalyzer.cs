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
            new(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage,
                title: @"Avoid use of assert method without message",
                message: "Only use Assert.True with message parameter",
                sourceClass: "Xunit.Assert",
                bannedMethod: "True",
                new[] {new[] {"bool"}}),
            new(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage,
                title: @"Avoid use of assert method without message",
                message: "Only use Assert.False with message parameter",
                sourceClass: "Xunit.Assert",
                bannedMethod: "False",
                new[] {new[] {"bool"}})
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
            IReadOnlyDictionary<Mapping, IReadOnlyList<IMethodSymbol>> cachedSymbols = BuildCachedSymbols(compilationStartContext.Compilation);

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                InvocationExpressionSyntax[] invocations = syntaxNodeAnalysisContext.Node.DescendantNodesAndSelf()
                                                                                    .OfType<InvocationExpressionSyntax>()
                                                                                    .ToArray();

                foreach (InvocationExpressionSyntax invocation in invocations)
                {
                    IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                    // check if there is at least one rule that correspond to invocation method
                    if (memberSymbol == null)
                    {
                        continue;
                    }

                    Mapping mapping = new(className: SymbolDisplay.ToDisplayString(memberSymbol.ContainingType), methodName: memberSymbol.Name);

                    if (!cachedSymbols.TryGetValue(key: mapping, out IReadOnlyList<IMethodSymbol> allowedMethodSignatures))
                    {
                        continue;
                    }

                    IEnumerable<ProhibitedMethodsSpec> prohibitedMethods = BannedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName);

                    foreach (ProhibitedMethodsSpec prohibitedMethod in prohibitedMethods)
                    {
                        if (!IsInvocationAllowed(invocationArguments: memberSymbol, methodSignatures: allowedMethodSignatures))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: prohibitedMethod.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.MethodDeclaration);
        }

        private static IReadOnlyDictionary<Mapping, IReadOnlyList<IMethodSymbol>> BuildCachedSymbols(Compilation compilation)
        {
            Dictionary<Mapping, IReadOnlyList<IMethodSymbol>> cachedSymbols = new();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                Mapping mapping = new(className: rule.SourceClass, methodName: rule.BannedMethod);

                if (cachedSymbols.ContainsKey(mapping))
                {
                    continue;
                }

                INamedTypeSymbol? sourceClassType = compilation.GetTypeByMetadataName(rule.SourceClass);

                if (sourceClassType == null || sourceClassType.GetMembers() == default)
                {
                    continue;
                }

                IMethodSymbol[] methodSignatures = sourceClassType.GetMembers()
                                                                  .Where(predicate: x => x.Name == rule.BannedMethod)
                                                                  .OfType<IMethodSymbol>()
                                                                  .ToArray();

                if (methodSignatures.Length > 0)
                {
                    cachedSymbols.Add(key: mapping, GetAllowedSignaturesForMethod(methodSignatures: methodSignatures, ruleSignatures: rule.BannedSignatures));
                }
            }

            return cachedSymbols;
        }

        /// <summary>
        ///     Filter method signatures to get only signatures allowed by rule
        /// </summary>
        /// <param name="methodSignatures">All signatures of one method</param>
        /// <param name="ruleSignatures">All banned signatures</param>
        /// <returns>Collection of allowed signatures.</returns>
        private static IReadOnlyList<IMethodSymbol> GetAllowedSignaturesForMethod(IEnumerable<IMethodSymbol> methodSignatures, IEnumerable<IEnumerable<string>> ruleSignatures)
        {
            if (methodSignatures == null)
            {
                throw new ArgumentException(message: "Unknown method signature");
            }

            if (ruleSignatures == null)
            {
                throw new ArgumentException(message: "Unknown rule signature");
            }

            List<IMethodSymbol> methodSignatureList = methodSignatures.ToList();

            foreach (IEnumerable<string> ruleSignature in ruleSignatures)
            {
                methodSignatureList.RemoveAll(match: methodSymbol => methodSymbol
                                                                     .Parameters.Select(selector: parameterSymbol => SymbolDisplay.ToDisplayString(parameterSymbol.Type))
                                                                     .SequenceEqual(ruleSignature));
            }

            return methodSignatureList;
        }

        /// <summary>
        ///     Check if invoked method signature is in list of allowed signatures
        /// </summary>
        /// <param name="invocationArguments">Arguments used in invocation of method</param>
        /// <param name="methodSignatures">List of all valid signatures for method</param>
        /// <returns>true, if the method was allowed; otherwise, false.</returns>
        private static bool IsInvocationAllowed(IMethodSymbol invocationArguments, IEnumerable<IMethodSymbol> methodSignatures)
        {
            return methodSignatures.Any(predicate: methodSignature => methodSignature.Parameters.SequenceEqual(invocationArguments.Parameters));
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