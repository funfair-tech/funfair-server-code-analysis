using System.Collections.Generic;
using System.Collections.Immutable;
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
///     Looks for methods which we want to prohibit with strict parameter invocation
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ProhibitedMethodsSpec[] ForcedMethods =
    {
        new(ruleId: Rules.RuleDontUseSubstituteReceivedWithZeroNumberOfCalls,
            title: "Avoid use of received with zero call count",
            message: "Only use Received with expected call count greater than 0, use DidNotReceived instead if 0 call received expected",
            sourceClass: "NSubstitute.SubstituteExtensions",
            forcedMethod: "Received",
            new[]
            {
                new[]
                {
                    new ParameterSpec(name: "requiredNumberOfCalls", type: "NumericLiteralExpression", value: "0")
                }
            }),
        new(ruleId: Rules.RuleDontUseConfigurationBuilderAddJsonFileWithReload,
            title: "Avoid use of reloadOnChange with value true",
            message: "Only use AddJsonFile with reloadOnChange set to false",
            sourceClass: "Microsoft.Extensions.Configuration.JsonConfigurationExtensions",
            forcedMethod: "AddJsonFile",
            new[]
            {
                new[]
                {
                    new ParameterSpec(name: "reloadOnChange", type: "TrueLiteralExpression", value: "true")
                }
            })
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
            if (syntaxNodeAnalysisContext.Node is InvocationExpressionSyntax invocation)
            {
                IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                // check if there is at least one rule that correspond to invocation method
                if (memberSymbol is null)
                {
                    return;
                }

                Mapping mapping = new(methodName: memberSymbol.Name, SymbolDisplay.ToDisplayString(memberSymbol.ContainingType));

                IEnumerable<ProhibitedMethodsSpec> forcedMethods = ForcedMethods.Where(predicate: rule => rule.QualifiedName == mapping.QualifiedName);

                foreach (ProhibitedMethodsSpec prohibitedMethod in forcedMethods)
                {
                    if (!IsInvocationAllowed(arguments: invocation.ArgumentList, parameters: memberSymbol.Parameters, prohibitedMethod: prohibitedMethod))
                    {
                        invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: prohibitedMethod.Rule);
                    }
                }
            }
        }

        compilationStartContext.RegisterSyntaxNodeAction(action: LookForForcedMethods, SyntaxKind.InvocationExpression);
    }

    /// <summary>
    ///     Check if invoked method is using proper arguments and values for them
    /// </summary>
    /// <param name="arguments">Arguments used in invocation of method</param>
    /// <param name="parameters">Method parameters</param>
    /// <param name="prohibitedMethod">Prohibited method rule</param>
    /// <returns></returns>
    private static bool IsInvocationAllowed(BaseArgumentListSyntax arguments, IReadOnlyList<IParameterSymbol> parameters, ProhibitedMethodsSpec prohibitedMethod)
    {
        if (!prohibitedMethod.BannedSignatures.Any())
        {
            return true;
        }

        // Thi needs to be simplified
        return !prohibitedMethod.BannedSignatures.SelectMany(collectionSelector: bannedSignature => bannedSignature,
                                                             resultSelector: (bannedSignature, parameterSpec) => new { bannedSignature, parameterSpec })
                                .Select(t => new { t, parameter = parameters.FirstOrDefault(predicate: param => param.MetadataName == t.parameterSpec.Name) })
                                .Where(t => t.parameter is not null)
                                .Select(t => new { t, argument = arguments.Arguments[t.parameter!.Ordinal] })
                                .Where(t => t.argument.Expression.ToFullString() == t.t.t.parameterSpec.Value && t.argument.Expression.Kind()
                                                                                                                  .ToString() == t.t.t.parameterSpec.Type)
                                .Select(t => t.t.t.parameterSpec)
                                .Any();
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
        public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string forcedMethod, IEnumerable<IEnumerable<ParameterSpec>> bannedSignatures)
        {
            this.SourceClass = sourceClass;
            this.ForcedMethod = forcedMethod;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.ProhibitedMethodWithStrictInvocations, title: title, message: message);
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