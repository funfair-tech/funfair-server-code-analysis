using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
            new ProhibitedMethodsSpec(Rules.RuleDontUseAssertTrueWithoutMessage,
                                      title: @"Avoid use of assert method without message",
                                      message: "Only use Assert.True with message parameter",
                                      sourceClass: "Xunit.Assert",
                                      bannedMethod: "True",
                                      new[] {new[] {"bool"}}),
            new ProhibitedMethodsSpec(Rules.RuleDontUseAssertFalseWithoutMessage,
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
            Dictionary<string, List<IMethodSymbol>> cachedSymbols = BuildCachedSymbols(compilationStartContext.Compilation);

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                InvocationExpressionSyntax[] invocations = syntaxNodeAnalysisContext.Node.DescendantNodesAndSelf()
                                                                                    .OfType<InvocationExpressionSyntax>()
                                                                                    .ToArray();

                foreach (InvocationExpressionSyntax invocation in invocations)
                {
                    string invokedMethod = invocation.Expression.ToString();

                    IMethodSymbol? memberSymbol = FindInvokedMemberSymbol(invocation, syntaxNodeAnalysisContext);

                    // check if there is at least on rule that correspond to invocation method
                    if (memberSymbol == null || !cachedSymbols.TryGetValue(invokedMethod, out List<IMethodSymbol> allowedMethodSignatures))
                    {
                        continue;
                    }

                    IEnumerable<ProhibitedMethodsSpec> prohibitedMethods = BannedMethods.Where(predicate: rule => rule.QualifiedName == invokedMethod);

                    foreach (ProhibitedMethodsSpec prohibitedMethod in prohibitedMethods)
                    {
                        if (!IsInvocationAllowed(memberSymbol, allowedMethodSignatures))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(prohibitedMethod.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(LookForBannedMethods, SyntaxKind.MethodDeclaration);
        }

        private static IMethodSymbol? FindInvokedMemberSymbol(InvocationExpressionSyntax invocation, SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            MemberAccessExpressionSyntax? memberAccessExpressionSyntax = invocation.Expression as MemberAccessExpressionSyntax;

            if (memberAccessExpressionSyntax == null)
            {
                return null;
            }

            IMethodSymbol? memberSymbol = syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(memberAccessExpressionSyntax)
                                                                   .Symbol as IMethodSymbol;

            return memberSymbol;
        }

        private static Dictionary<string, List<IMethodSymbol>> BuildCachedSymbols(Compilation compilation)
        {
            Dictionary<string, List<IMethodSymbol>> cachedSymbols = new Dictionary<string, List<IMethodSymbol>>();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (cachedSymbols.ContainsKey(rule.SourceClass))
                {
                    continue;
                }

                INamedTypeSymbol sourceClassType = compilation.GetTypeByMetadataName(rule.SourceClass);

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
                    cachedSymbols.Add(rule.SourceClass, GetAllowedSignaturesForMethod(methodSignatures, rule.BannedSignatures));
                }
            }

            return cachedSymbols;
        }

        /// <summary>
        ///     Filter method signatures to get only signatures allowed by rule
        /// </summary>
        /// <param name="methodSignatures">All signatures of one method</param>
        /// <param name="ruleSignatures">All banned signatures</param>
        /// <returns></returns>
        private static List<IMethodSymbol> GetAllowedSignaturesForMethod(IEnumerable<IMethodSymbol> methodSignatures, IEnumerable<IEnumerable<string>> ruleSignatures)
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
                methodSignatureList.RemoveAll(match: methodSymbol => methodSymbol.Parameters.Select(selector: parameterSymbol => parameterSymbol.Type.ToString())
                                                                                 .SequenceEqual(ruleSignature));
            }

            return methodSignatureList;
        }

        /// <summary>
        ///     Check if invoked method signature is in list of allowed signatures
        /// </summary>
        /// <param name="invocationArguments">Arguments used in invocation of method</param>
        /// <param name="methodSignatures">List of all valid signatures for method</param>
        /// <returns></returns>
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
                this.Rule = CreateRule(ruleId, title, message);
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
            public string QualifiedName => $"{this.SourceClass}.{this.BannedMethod}";

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(title);
                LiteralString translatableMessage = new LiteralString(message);

                return new DiagnosticDescriptor(code, translatableTitle, translatableMessage, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true, translatableMessage);
            }
        }
    }
}