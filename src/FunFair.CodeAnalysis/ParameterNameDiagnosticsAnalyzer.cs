using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for issues with parameter names
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ParameterNameDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<NameSanitationSpec> NameSpecifications = new NameSanitationSpec[]
                                                                                   {
                                                                                       new(ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
                                                                                           title: @"ILogger parameters should be called 'logger'",
                                                                                           message: "ILogger parameters should be called 'logger'",
                                                                                           sourceClass: "Microsoft.Extensions.Logging.ILogger",
                                                                                           whitelistedParameterName: "logger"),
                                                                                       new(ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
                                                                                           title: @"ILogger parameters should be called 'logger'",
                                                                                           message: "ILogger parameters should be called 'logger'",
                                                                                           sourceClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                                                                                           whitelistedParameterName: "logger")
                                                                                   };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        NameSpecifications.Select(selector: r => r.Rule)
                          .ToImmutableArray();

    /// <inheritdoc />
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
        if (syntaxNodeAnalysisContext.Node is ParameterSyntax parameterSyntax)
        {
            string? fullTypeName = ParameterHelpers.GetFullTypeName(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax);

            if (fullTypeName != null)
            {
                NameSanitationSpec? rule = NameSpecifications.FirstOrDefault(ns => ns.SourceClass == fullTypeName);

                if (rule != null)
                {
                    if (!rule.WhitelistedParameterNames.Contains(value: parameterSyntax.Identifier.Text, comparer: StringComparer.Ordinal))
                    {
                        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule.Rule, parameterSyntax.GetLocation()));
                    }
                }
            }
        }
    }

    private sealed class NameSanitationSpec
    {
        public NameSanitationSpec(string ruleId, string title, string message, string sourceClass, string whitelistedParameterName)
            : this(ruleId: ruleId,
                   title: title,
                   message: message,
                   sourceClass: sourceClass,
                   new[]
                   {
                       whitelistedParameterName
                   })
        {
        }

        public NameSanitationSpec(string ruleId, string title, string message, string sourceClass, string[] whitelistedParameterNames)
        {
            this.SourceClass = sourceClass;
            this.WhitelistedParameterNames = whitelistedParameterNames;

            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.Naming, title: title, message: message);
        }

        public string SourceClass { get; }

        public IReadOnlyList<string> WhitelistedParameterNames { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}