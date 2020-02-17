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
                                      sourceClass: "XUnit.Assert",
                                      bannedMethod: "True",
                                      new[] {new[] {"bool"}}),
            new ProhibitedMethodsSpec(Rules.RuleDontUseAssertFalseWithoutMessage,
                                      title: @"Avoid use of assert method without message",
                                      message: "Only use Assert.False with message parameter",
                                      sourceClass: "XUnit.Assert",
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
            Dictionary<string, IEnumerable<IMethodSymbol>> cachedSymbols = new Dictionary<string, IEnumerable<IMethodSymbol>>();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (cachedSymbols.ContainsKey(rule.QualifiedName))
                {
                    continue;
                }

                IMethodSymbol[] methodSignatures = compilationStartContext.Compilation.GetTypeByMetadataName(rule.SourceClass)
                                                                          .GetMembers()
                                                                          .Where(predicate: x => x.Name == rule.BannedMethod)
                                                                          .OfType<IMethodSymbol>()
                                                                          .ToArray();

                if (methodSignatures.Length > 0)
                {
                    cachedSymbols.Add(key: rule.QualifiedName, value: GetAllowedSignaturesForMethod(methodSignatures, rule.BannedSignatures));
                }
            }

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                IEnumerable<InvocationExpressionSyntax> invocations = syntaxNodeAnalysisContext.Node.DescendantNodesAndSelf()
                                                                                               .OfType<InvocationExpressionSyntax>();

                foreach (InvocationExpressionSyntax invocation in invocations)
                {
                    string invokedMethod = invocation.Expression.ToString();

                    MemberAccessExpressionSyntax? memberAccessExpr = invocation.Expression as MemberAccessExpressionSyntax;
                    IMethodSymbol? memberSymbol = syntaxNodeAnalysisContext.SemanticModel.GetSymbolInfo(memberAccessExpr)
                                                                          .Symbol as IMethodSymbol;

                    // check if there is at least on rule that correspond to invocation method
                    if (memberSymbol == null || !cachedSymbols.TryGetValue(invokedMethod, out IEnumerable<IMethodSymbol> allowedMethodSignatures))
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

        /// <summary>
        ///     Filter method signatures to get only signatures allowed by rule
        /// </summary>
        /// <param name="methodSignatures">All signatures of one method</param>
        /// <param name="ruleSignatures">All banned signatures</param>
        /// <returns></returns>
        private static IEnumerable<IMethodSymbol> GetAllowedSignaturesForMethod(IEnumerable<IMethodSymbol> methodSignatures, IEnumerable<IEnumerable<string>> ruleSignatures)
        {
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

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(title);
                LiteralString translatableMessage = new LiteralString(message);

                return new DiagnosticDescriptor(code, translatableTitle, translatableMessage, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true, translatableMessage);
            }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => $"{this.SourceClass}.{this.BannedMethod}";
        }
    }
}