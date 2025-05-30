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

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [.. NameSpecifications.Select(selector: r => r.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: MustHaveASaneName, SyntaxKind.Parameter);
    }

    private static void MustHaveASaneName(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not ParameterSyntax parameterSyntax)
        {
            return;
        }

        MustHaveASaneName(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            parameterSyntax: parameterSyntax,
            cancellationToken: syntaxNodeAnalysisContext.CancellationToken
        );
    }

    private static void MustHaveASaneName(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        ParameterSyntax parameterSyntax,
        CancellationToken cancellationToken
    )
    {
        string? fullTypeName = ParameterHelpers.GetFullTypeName(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            parameterSyntax: parameterSyntax,
            cancellationToken: cancellationToken
        );

        if (fullTypeName is null)
        {
            return;
        }

        MustHaveASaneName(
            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
            parameterSyntax: parameterSyntax,
            fullTypeName: fullTypeName
        );
    }

    private static void MustHaveASaneName(
        in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
        ParameterSyntax parameterSyntax,
        string fullTypeName
    )
    {
        NameSanitationSpec? rule = FindSpec(fullTypeName);

        if (!rule.HasValue)
        {
            return;
        }

        if (
            !rule.Value.WhitelistedParameterNames.Contains(
                value: parameterSyntax.Identifier.Text,
                comparer: StringComparer.Ordinal
            )
        )
        {
            parameterSyntax.ReportDiagnostics(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                rule: rule.Value.Rule
            );
        }
    }

    [SuppressMessage(category: "SonarAnalyzer.CSharp", checkId: "S3267: Use Linq", Justification = "Not here")]
    private static NameSanitationSpec? FindSpec(string fullTypeName)
    {
        foreach (NameSanitationSpec ns in NameSpecifications)
        {
            if (StringComparer.Ordinal.Equals(x: ns.SourceClass, y: fullTypeName))
            {
                return ns;
            }
        }

        return null;
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
