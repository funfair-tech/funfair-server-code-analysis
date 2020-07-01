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
    ///     Looks for methods which we want to enforce amount of parameters used (if there are optional parameters)
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ForceMethodParametersInvocationsDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Forced Method Invocations";

        private static readonly ForcedMethodsSpec[] ForcedMethods =
        {
            new ForcedMethodsSpec(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
                                  title: @"Avoid use of serializer without options",
                                  message: "Only use JsonSerializer.Serialize with JsonSerializerOptions parameter",
                                  sourceClass: "System.Text.Json.JsonSerializer",
                                  forcedMethod: "Serialize",
                                  new[] {new[] {"TValue", "JsonOptionsSerializer"}},
                                  requiredArgumentCount: 2),
            new ForcedMethodsSpec(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions,
                                  title: @"Avoid use of serializer without options",
                                  message: "Only use JsonSerializer.Serialize with JsonSerializerOptions parameter",
                                  sourceClass: "System.Text.Json.JsonSerializer",
                                  forcedMethod: "SerializeAsync",
                                  new[] {new[] {"TValue", "JsonOptionsSerializer"}},
                                  requiredArgumentCount: 2),
            new ForcedMethodsSpec(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
                                  title: @"Avoid use of deserializer without options",
                                  message: "Only use JsonSerializer.Deserialize with JsonSerializerOptions parameter",
                                  sourceClass: "System.Text.Json.JsonSerializer",
                                  forcedMethod: "Deserialize",
                                  new[] {new[] {"string", "JsonOptionsSerializer"}},
                                  requiredArgumentCount: 2),
            new ForcedMethodsSpec(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions,
                                  title: @"Avoid use of deserializer without options",
                                  message: "Only use JsonSerializer.Deserialize with JsonSerializerOptions parameter",
                                  sourceClass: "System.Text.Json.JsonSerializer",
                                  forcedMethod: "DeserializeAsync",
                                  new[] {new[] {"Stream", "JsonOptionsSerializer", "CancellationToken"}},
                                  requiredArgumentCount: 3)
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ForcedMethods.Select(selector: r => r.Rule)
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
            void LookForForcedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
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

                    Mapping mapping = new Mapping(className: memberSymbol.ContainingNamespace + "." + memberSymbol.ContainingType.Name, methodName: memberSymbol.Name);

                    IEnumerable<ForcedMethodsSpec> forcedMethods = ForcedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName);

                    foreach (ForcedMethodsSpec prohibitedMethod in forcedMethods)
                    {
                        if (!IsInvocationAllowed(invocationArguments: memberSymbol,
                                                 argumentsInvokedCount: invocation.ArgumentList.Arguments.Count,
                                                 requiredArgumentsCount: prohibitedMethod.RequiredArgumentCount))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: prohibitedMethod.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForForcedMethods, SyntaxKind.MethodDeclaration);
        }

        /// <summary>
        ///     Check if invoked method is invoked as per rules
        /// </summary>
        /// <param name="invocationArguments">Arguments used in invocation of method</param>
        /// <param name="requiredArgumentsCount">Required arguments count</param>
        /// <param name="argumentsInvokedCount">Method invoked with argument amount</param>
        /// <returns>true, if the method was allowed; otherwise, false.</returns>
        private static bool IsInvocationAllowed(IMethodSymbol invocationArguments, int argumentsInvokedCount, int requiredArgumentsCount)
        {
            bool allowedBasedOnArgumentTypeAndSequence = invocationArguments.Parameters.SequenceEqual(invocationArguments.Parameters);
            bool allowedBasedOnArgumentCount = argumentsInvokedCount == requiredArgumentsCount;

            return allowedBasedOnArgumentCount && allowedBasedOnArgumentTypeAndSequence;
        }

        private sealed class ForcedMethodsSpec
        {
            public ForcedMethodsSpec(string ruleId, string title, string message, string sourceClass, string forcedMethod, IEnumerable<IEnumerable<string>> forcedSignatures, int requiredArgumentCount)
            {
                this.SourceClass = sourceClass;
                this.ForcedMethod = forcedMethod;
                this.Rule = CreateRule(code: ruleId, title: title, message: message);
                this.ForcedSignatures = forcedSignatures;
                this.RequiredArgumentCount = requiredArgumentCount;
            }

            public string SourceClass { get; }

            public string ForcedMethod { get; }

            /// <summary>
            ///     List of all method signatures that are banned, every signature is given with array of types in exact parameter order
            /// </summary>
            public IEnumerable<IEnumerable<string>> ForcedSignatures { get; }

            public int RequiredArgumentCount { get; }

            public DiagnosticDescriptor Rule { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);

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