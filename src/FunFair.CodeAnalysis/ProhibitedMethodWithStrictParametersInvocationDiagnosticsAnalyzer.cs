using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedMethodWithStrictParametersInvocationDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<ProhibitedMethodsSpec> ForcedMethods =
    [
        Build(
            ruleId: Rules.RuleDontUseSubstituteReceivedWithZeroNumberOfCalls,
            title: "Avoid use of received with zero call count",
            message: "Only use Received with expected call count greater than 0, use DidNotReceived instead if 0 call received expected",
            sourceClass: "NSubstitute.SubstituteExtensions",
            forcedMethod: "Received",
            [
                [Build(name: "requiredNumberOfCalls", type: "NumericLiteralExpression", value: "0")],
            ]
        ),
        Build(
            ruleId: Rules.RuleDontUseConfigurationBuilderAddJsonFileWithReload,
            title: "Avoid use of reloadOnChange with value true",
            message: "Only use AddJsonFile with reloadOnChange set to false",
            sourceClass: "Microsoft.Extensions.Configuration.JsonConfigurationExtensions",
            forcedMethod: "AddJsonFile",
            [
                [Build(name: "reloadOnChange", type: "TrueLiteralExpression", value: "true")],
            ]
        ),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [.. ForcedMethods.Select(selector: r => r.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: LookForForcedMethods, SyntaxKind.InvocationExpression);
    }

    private static bool FindInvokedMemberSymbol(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        out InvocationExpressionSyntax? invocation,
        out IMethodSymbol? memberSymbol
    )
    {
        if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax i)
        {
            invocation = null;
            memberSymbol = null;

            return false;
        }

        memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(
            invocation: i,
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
        );

        // check if there is at least one rule that correspond to invocation method
        if (memberSymbol is null)
        {
            invocation = null;

            return false;
        }

        invocation = i;

        return true;
    }

    private static void LookForForcedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (
            !FindInvokedMemberSymbol(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                out InvocationExpressionSyntax? invocation,
                out IMethodSymbol? memberSymbol
            )
        )
        {
            return;
        }

        // ! memberSymbol is guaranteed to be not null here
        Mapping mapping = new(
            methodName: memberSymbol!.Name,
            SymbolDisplay.ToDisplayString(memberSymbol.ContainingType)
        );

        IEnumerable<ProhibitedMethodsSpec> forcedMethods = ForcedMethods.Where(predicate: rule =>
            StringComparer.Ordinal.Equals(x: rule.QualifiedName, y: mapping.QualifiedName)
        );

        // ! Invocation is guaranteed to be not null here
        foreach (
            ProhibitedMethodsSpec prohibitedMethod in forcedMethods.Where(prohibitedMethod =>
                !IsInvocationAllowed(
                    arguments: invocation!.ArgumentList,
                    parameters: memberSymbol.Parameters,
                    prohibitedMethod: prohibitedMethod
                )
            )
        )
        {
            // ! Invocation is guaranteed to be not null here
            invocation!.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: prohibitedMethod.Rule
            );
        }
    }

    private static bool IsInvocationAllowed(
        BaseArgumentListSyntax arguments,
        IReadOnlyList<IParameterSymbol> parameters,
        in ProhibitedMethodsSpec prohibitedMethod
    )
    {
        if (prohibitedMethod.BannedSignatures.Count == 0)
        {
            return true;
        }

        // This needs to be simplified
        // ! Nullable is guaranteed to be not null here
        return !prohibitedMethod
            .BannedSignatures.SelectMany(
                collectionSelector: bannedSignature => bannedSignature,
                resultSelector: (bannedSignature, parameterSpec) => (bannedSignature, parameterSpec)
            )
            .Select(t =>
                (
                    t,
                    parameter: parameters.FirstOrDefault(predicate: param =>
                        StringComparer.Ordinal.Equals(x: param.MetadataName, y: t.parameterSpec.Name)
                    )
                )
            )
            .Where(t => t.parameter is not null)
            .Select(t => (t, argument: arguments.Arguments[t.parameter!.Ordinal]))
            .Where(t =>
                StringComparer.Ordinal.Equals(t.argument.Expression.ToFullString(), y: t.t.t.parameterSpec.Value)
                && StringComparer.Ordinal.Equals(t.argument.Expression.Kind().ToString(), y: t.t.t.parameterSpec.Type)
            )
            .Select(t => t.t.t.parameterSpec)
            .Any();
    }

    private static ParameterSpec Build(string name, string type, string value)
    {
        return new(name: name, type: type, value: value);
    }

    private static ProhibitedMethodsSpec Build(
        string ruleId,
        string title,
        string message,
        string sourceClass,
        string forcedMethod,
        IReadOnlyList<IReadOnlyList<ParameterSpec>> bannedSignatures
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            sourceClass: sourceClass,
            forcedMethod: forcedMethod,
            bannedSignatures: bannedSignatures
        );
    }

    [DebuggerDisplay("{Name}:{Type} = {Value}")]
    private readonly record struct ParameterSpec
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

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {SourceClass} Forced Method: {ForcedMethod}")]
    private readonly record struct ProhibitedMethodsSpec
    {
        public ProhibitedMethodsSpec(
            string ruleId,
            string title,
            string message,
            string sourceClass,
            string forcedMethod,
            IReadOnlyList<IReadOnlyList<ParameterSpec>> bannedSignatures
        )
        {
            this.SourceClass = sourceClass;
            this.ForcedMethod = forcedMethod;
            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.ProhibitedMethodWithStrictInvocations,
                title: title,
                message: message
            );
            this.BannedSignatures = bannedSignatures;
        }

        public string SourceClass { get; }

        public string ForcedMethod { get; }

        public IReadOnlyList<IReadOnlyList<ParameterSpec>> BannedSignatures { get; }

        public DiagnosticDescriptor Rule { get; }

        public string QualifiedName => string.Concat(str0: this.SourceClass, str1: ".", str2: this.ForcedMethod);
    }
}
