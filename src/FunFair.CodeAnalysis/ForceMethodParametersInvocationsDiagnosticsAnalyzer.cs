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
            new(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions, title: @"Avoid use of serializer without own JsonSerializerOptions parameter", message:
                "Only use JsonSerializer.Serialize with own JsonSerializerOptions", sourceClass: "System.Text.Json.JsonSerializer", forcedMethod: "Serialize",
                requiredArgumentCount: 2),
            new(ruleId: Rules.RuleDontUseJsonSerializerWithoutJsonOptions, title: @"Avoid use of serializer without own JsonSerializerOptions parameter", message:
                "Only use JsonSerializer.Serialize with own JsonSerializerOptions", sourceClass: "System.Text.Json.JsonSerializer", forcedMethod: "SerializeAsync",
                requiredArgumentCount: 2),
            new(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions, title: @"Avoid use of deserializer without own JsonSerializerOptions parameter", message:
                "Only use JsonSerializer.Deserialize with own JsonSerializerOptions", sourceClass: "System.Text.Json.JsonSerializer", forcedMethod: "Deserialize",
                requiredArgumentCount: 2),
            new(ruleId: Rules.RuleDontUseJsonDeserializerWithoutJsonOptions, title: @"Avoid use of deserializer without own JsonSerializerOptions parameter", message:
                "Only use JsonSerializer.Deserialize with own JsonSerializerOptions", sourceClass: "System.Text.Json.JsonSerializer", forcedMethod: "DeserializeAsync",
                requiredArgumentCount: 2),
            new(ruleId: Rules.RuleDontUseSubstituteReceivedWithoutAmountOfCalls, title: @"Avoid use of received without call count", message:
                "Only use Received with expected call count", sourceClass: "NSubstitute.SubstituteExtensions", forcedMethod: "Received", requiredArgumentCount: 1)
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

                    Mapping mapping = new(methodName: memberSymbol.Name, SymbolDisplay.ToDisplayString(memberSymbol.ContainingType));

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
        /// <param name="argumentsInvokedCount">Method invoked with argument amount</param>
        /// <param name="requiredArgumentsCount">Required arguments count</param>
        /// <returns>true, if the method was allowed; otherwise, false.</returns>
        private static bool IsInvocationAllowed(IMethodSymbol invocationArguments, int argumentsInvokedCount, int requiredArgumentsCount)
        {
            bool allowedBasedOnArgumentTypeAndSequence = invocationArguments.Parameters.SequenceEqual(invocationArguments.Parameters);
            bool allowedBasedOnArgumentCount = argumentsInvokedCount >= requiredArgumentsCount;

            return allowedBasedOnArgumentCount && allowedBasedOnArgumentTypeAndSequence;
        }

        private sealed class ForcedMethodsSpec
        {
            public ForcedMethodsSpec(string ruleId, string title, string message, string sourceClass, string forcedMethod, int requiredArgumentCount)
            {
                this.SourceClass = sourceClass;
                this.ForcedMethod = forcedMethod;
                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
                this.RequiredArgumentCount = requiredArgumentCount;
            }

            public string SourceClass { get; }

            public string ForcedMethod { get; }

            public int RequiredArgumentCount { get; }

            public DiagnosticDescriptor Rule { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);
        }
    }
}