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
    ///     Looks for methods which we want to prohibit with strict parameter invocation
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Prohibited Method With Strict Invocations";

        private static readonly ProhibitedMethodsSpec[] ForcedMethods =
        {
            new(ruleId: Rules.RuleDontUseSubstituteReceivedWithZeroNumberOfCalls,
                title: "Avoid use of received with zero call count",
                message: "Only use Received with expected call count greater than 0, use DidNotReceived instead if 0 call received expected",
                sourceClass: "NSubstitute.SubstituteExtensions",
                forcedMethod: "Received",
                new[] {new[] {new ParameterSpec(name: "requiredNumberOfCalls", type: "NumericLiteralExpression", value: "0")}})
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

                    IEnumerable<ProhibitedMethodsSpec> forcedMethods = ForcedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName);

                    foreach (ProhibitedMethodsSpec prohibitedMethod in forcedMethods)
                    {
                        if (!IsInvocationAllowed(arguments: invocation.ArgumentList, parameters: memberSymbol.Parameters, prohibitedMethod: prohibitedMethod))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: prohibitedMethod.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForForcedMethods, SyntaxKind.MethodDeclaration);
        }

        /// <summary>
        ///     Check if invoked method is using proper arguments and values for them
        /// </summary>
        /// <param name="arguments">Arguments used in invocation of method</param>
        /// <param name="parameters">Method parameters</param>
        /// <param name="prohibitedMethod">Prohibited method rule</param>
        /// <returns></returns>
        private static bool IsInvocationAllowed(BaseArgumentListSyntax arguments, ImmutableArray<IParameterSymbol> parameters, ProhibitedMethodsSpec prohibitedMethod)
        {
            if (!prohibitedMethod.BannedSignatures.Any())
            {
                return true;
            }

            foreach (IEnumerable<ParameterSpec> bannedSignature in prohibitedMethod.BannedSignatures)
            {
                foreach (ParameterSpec? parameterSpec in bannedSignature)
                {
                    IParameterSymbol? parameter = parameters.FirstOrDefault(predicate: param => param.MetadataName == parameterSpec.Name);

                    if (parameter == null)
                    {
                        continue;
                    }

                    ArgumentSyntax? argument = arguments.Arguments[parameter.Ordinal];

                    if (argument == null)
                    {
                        continue;
                    }

                    if (argument.Expression.ToFullString() == parameterSpec.Value && argument.Expression.Kind()
                                                                                             .ToString() == parameterSpec.Type)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private sealed class ParameterSpec
        {
            public ParameterSpec(string name, string type, string value)
            {
                this.Name = name;
                this.Type = type;
                this.Value = value;
            }

            public string Name { get; }

            public string Type { get; }

            public string Value { get; }
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId,
                                         string title,
                                         string message,
                                         string sourceClass,
                                         string forcedMethod,
                                         IEnumerable<IEnumerable<ParameterSpec>> bannedSignatures)
            {
                this.SourceClass = sourceClass;
                this.ForcedMethod = forcedMethod;
                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
                this.BannedSignatures = bannedSignatures;
            }

            public string SourceClass { get; }

            public string ForcedMethod { get; }

            /// <summary>
            ///     List of all method signatures that are banned, every signature is given with array of types in exact parameter order
            /// </summary>
            public IEnumerable<IEnumerable<ParameterSpec>> BannedSignatures { get; }

            public DiagnosticDescriptor Rule { get; }

            /// <summary>
            ///     Full qualified name of method
            /// </summary>
            public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);
        }
    }
}