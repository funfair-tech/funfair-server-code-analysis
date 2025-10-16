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
public sealed class ProhibitedPragmasDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ImmutableHashSet<string> AllowedWarnings = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "1591" // Xml Docs
    );

    private static readonly ImmutableHashSet<string> AllowedInTestWarnings = ImmutableHashSet.Create(
        StringComparer.Ordinal,
        "1718" // Comparison made to the same variable; did you mean to compare something else?
    );

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleDontDisableWarnings,
        category: Categories.IllegalPragmas,
        title: "Don't disable warnings with #pragma warning disable",
        message: "Don't disable warnings using #pragma warning disable"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        Checker checker = new(compilationStartContext.Compilation.IsTestAssembly());

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.LookForBannedPragmas,
            SyntaxKind.PragmaWarningDirectiveTrivia
        );
    }

    private sealed class Checker
    {
        private readonly ImmutableHashSet<string> _allowedWarnings;

        public Checker(bool isTestAssembly)
        {
            this._allowedWarnings = isTestAssembly ? AllowedWarnings.Union(AllowedInTestWarnings) : AllowedWarnings;
        }

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void LookForBannedPragmas(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective)
            {
                return;
            }

            pragmaWarningDirective
                .ErrorCodes.Where(errorCode => !this._allowedWarnings.Contains(errorCode.ToString()))
                .ForEach(errorCode =>
                    errorCode.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule)
                );
        }
    }
}
