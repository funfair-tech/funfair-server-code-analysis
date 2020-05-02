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
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseAssertTrueWithoutMessage,
                                      title: @"Avoid use of assert method without message",
                                      message: "Only use Assert.True with message parameter",
                                      sourceClass: "Xunit.Assert",
                                      bannedMethod: "True",
                                      new[] {new[] {"bool"}}),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseAssertFalseWithoutMessage,
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
                    IMethodSymbol? memberSymbol = FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                    // check if there is at least on rule that correspond to invocation method
                    if (memberSymbol == null)
                    {
                        continue;
                    }

                    Mapping mapping = new Mapping(className: memberSymbol.ContainingNamespace.Name + "." + memberSymbol.ContainingType.Name, methodName: memberSymbol.Name);

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

        private static IReadOnlyDictionary<Mapping, IReadOnlyList<IMethodSymbol>> BuildCachedSymbols(Compilation compilation)
        {
            Dictionary<Mapping, IReadOnlyList<IMethodSymbol>> cachedSymbols = new Dictionary<Mapping, IReadOnlyList<IMethodSymbol>>();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                Mapping mapping = new Mapping(className: rule.SourceClass, methodName: rule.BannedMethod);

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
        /// <returns>true, if the method was allowed; otherwise, false.</returns>
        private static bool IsInvocationAllowed(IMethodSymbol invocationArguments, IEnumerable<IMethodSymbol> methodSignatures)
        {
            return methodSignatures.Any(predicate: methodSignature => methodSignature.Parameters.SequenceEqual(invocationArguments.Parameters));
        }

        private sealed class Mapping : IEquatable<Mapping>
        {
            public Mapping(string methodName, string className)
            {
                this.MethodName = methodName;
                this.ClassName = className;
            }

            public string MethodName { get; }

            public string ClassName { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.ClassName, str1: ".", str2: this.MethodName);

            public bool Equals(Mapping? other)
            {
                if (ReferenceEquals(objA: null, objB: other))
                {
                    return false;
                }

                if (ReferenceEquals(this, objB: other))
                {
                    return true;
                }

                return this.MethodName == other.MethodName && this.ClassName == other.ClassName;
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, objB: obj) || obj is Mapping other && this.Equals(other);
            }

            public override int GetHashCode()
            {
                return (this.MethodName.GetHashCode() * 397) ^ this.ClassName.GetHashCode();
            }

            public static bool operator ==(Mapping? left, Mapping? right)
            {
                return Equals(objA: left, objB: right);
            }

            public static bool operator !=(Mapping? left, Mapping? right)
            {
                return !Equals(objA: left, objB: right);
            }
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod, IEnumerable<IEnumerable<string>> bannedSignatures)
            {
                this.SourceClass = sourceClass;
                this.BannedMethod = bannedMethod;
                this.Rule = CreateRule(code: ruleId, title: title, message: message);
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

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(title);
                LiteralString translatableMessage = new LiteralString(message);

                return new DiagnosticDescriptor(id: code,
                                                title: translatableTitle,
                                                messageFormat: translatableMessage,
                                                category: CATEGORY,
                                                defaultSeverity: DiagnosticSeverity.Error,
                                                isEnabledByDefault: true,
                                                description: translatableMessage);
            }
        }
    }
}