
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly ImmutableHashSet<string> QualifiedMethodNames = ForcedMethods
        .Select(method => method.QualifiedName)
        .ToImmutableHashSet(StringComparer.Ordinal);

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
        Checker checker = new();
        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.LookForForcedMethods,
            SyntaxKind.InvocationExpression
        );
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

    private sealed class Checker
    {
        private readonly Dictionary<string, IReadOnlyList<ProhibitedMethodsSpec>> _qualifiedMethodCache = new(StringComparer.Ordinal);

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void LookForForcedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax invocation)
            {
                return;
            }

            IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(
                invocation: invocation,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
            );

            if (memberSymbol is null)
            {
                return;
            }

            string qualifiedName = $"{SymbolDisplay.ToDisplayString(memberSymbol.ContainingType)}.{memberSymbol.Name}";

            // Fast path: check if this qualified name is even in our list
            if (!QualifiedMethodNames.Contains(qualifiedName))
            {
                return;
            }

            IReadOnlyList<ProhibitedMethodsSpec> matchingProhibitedMethods = this.GetMatchingProhibitedMethods(qualifiedName);

            IEnumerable<ProhibitedMethodsSpec> violatingMethods = matchingProhibitedMethods
                .Where(prohibitedMethod => !IsInvocationAllowed(
                    arguments: invocation.ArgumentList,
                    parameters: memberSymbol.Parameters,
                    prohibitedMethod: prohibitedMethod
                ));

            foreach (ProhibitedMethodsSpec prohibitedMethod in violatingMethods)
            {
                invocation.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: prohibitedMethod.Rule
                );
            }
        }

        private IReadOnlyList<ProhibitedMethodsSpec> GetMatchingProhibitedMethods(string qualifiedName)
        {
            if (!this._qualifiedMethodCache.TryGetValue(key: qualifiedName, out IReadOnlyList<ProhibitedMethodsSpec>? cached))
            {
                cached =
                [
                    ..ForcedMethods.Where(rule => StringComparer.Ordinal.Equals(x: rule.QualifiedName, y: qualifiedName))
                ];

                this._qualifiedMethodCache[qualifiedName] = cached;
            }

            return cached;
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

            // ! Parameter is not null here
            return !prohibitedMethod.BannedSignatures
                .SelectMany(bannedSignature => bannedSignature)
                .Select(paramSpec => (
                    ParamSpec: paramSpec,
                    Parameter: parameters.FirstOrDefault(param =>
                        StringComparer.Ordinal.Equals(x: param.MetadataName, y: paramSpec.Name)
                    )
                ))
                .Where(tuple => tuple.Parameter is not null)
                .Select(tuple => (
                    tuple.ParamSpec,
                    Argument: arguments.Arguments[tuple.Parameter!.Ordinal]
                ))
                .Any(tuple =>
                    StringComparer.Ordinal.Equals(tuple.Argument.Expression.ToFullString(), y: tuple.ParamSpec.Value)
                    && StringComparer.Ordinal.Equals(tuple.Argument.Expression.Kind().ToString(), y: tuple.ParamSpec.Type)
                );
        }
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