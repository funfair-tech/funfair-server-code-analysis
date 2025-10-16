using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterNameDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<NameSanitationSpec> NameSpecifications =
    [
        Build(
            ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
            title: "ILogger parameters should be called 'logger'",
            message: "ILogger parameters should be called 'logger'",
            sourceClass: "Microsoft.Extensions.Logging.ILogger",
            whitelistedParameterName: "logger"
        ),
        Build(
            ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
            title: "ILogger parameters should be called 'logger'",
            message: "ILogger parameters should be called 'logger'",
            sourceClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
            whitelistedParameterName: "logger"
        ),
    ];

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
    [
        .. NameSpecifications.Select(r => r.Rule),
    ];

    private static readonly StringComparer ParameterNameComparer = StringComparer.Ordinal;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        Checker checker = new();

        compilationStartContext.RegisterSyntaxNodeAction(action: checker.MustHaveASaneName, SyntaxKind.Parameter);
    }

    private static NameSanitationSpec Build(
        string ruleId,
        string title,
        string message,
        string sourceClass,
        string whitelistedParameterName
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            sourceClass: sourceClass,
            whitelistedParameterName: whitelistedParameterName
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<string, NameSanitationSpec?> _specCache = new(StringComparer.Ordinal);
        private readonly Dictionary<ParameterSyntax, string?> _typeNameCache = [];

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void MustHaveASaneName(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not ParameterSyntax parameterSyntax)
            {
                return;
            }

            string? fullTypeName = this.GetFullTypeName(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parameterSyntax: parameterSyntax,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

            if (fullTypeName is null)
            {
                return;
            }

            NameSanitationSpec? rule = this.FindSpec(fullTypeName);

            if (!rule.HasValue)
            {
                return;
            }

            if (
                !rule.Value.WhitelistedParameterNames.Contains(
                    value: parameterSyntax.Identifier.Text,
                    comparer: ParameterNameComparer
                )
            )
            {
                parameterSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: rule.Value.Rule
                );
            }
        }

        private string? GetFullTypeName(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ParameterSyntax parameterSyntax,
            CancellationToken cancellationToken
        )
        {
            if (this._typeNameCache.TryGetValue(key: parameterSyntax, out string? cachedTypeName))
            {
                return cachedTypeName;
            }

            string? typeName = ParameterHelpers.GetFullTypeName(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parameterSyntax: parameterSyntax,
                cancellationToken: cancellationToken
            );

            this._typeNameCache[parameterSyntax] = typeName;
            return typeName;
        }

        private NameSanitationSpec? FindSpec(string fullTypeName)
        {
            if (this._specCache.TryGetValue(key: fullTypeName, out NameSanitationSpec? cachedSpec))
            {
                return cachedSpec;
            }

            NameSanitationSpec? spec = NameSpecifications.FirstOrDefault(ns =>
                StringComparer.Ordinal.Equals(x: ns.SourceClass, y: fullTypeName)
            );

            this._specCache[fullTypeName] = spec;
            return spec;
        }
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {SourceClass}")]
    private readonly record struct NameSanitationSpec
    {
        public NameSanitationSpec(
            string ruleId,
            string title,
            string message,
            string sourceClass,
            string whitelistedParameterName
        )
            : this(ruleId: ruleId, title: title, message: message, sourceClass: sourceClass, [whitelistedParameterName])
        { }

        public NameSanitationSpec(
            string ruleId,
            string title,
            string message,
            string sourceClass,
            IReadOnlyList<string> whitelistedParameterNames
        )
        {
            this.SourceClass = sourceClass;
            this.WhitelistedParameterNames = whitelistedParameterNames;

            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.Naming,
                title: title,
                message: message
            );
        }

        public string SourceClass { get; }

        public IReadOnlyList<string> WhitelistedParameterNames { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}
