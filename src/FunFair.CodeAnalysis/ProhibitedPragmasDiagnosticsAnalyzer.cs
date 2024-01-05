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
    private static readonly string[] AllowedWarnings =
    [
        // Xml Docs
        "1591"
    ];

    private static readonly string[] AllowedInTestWarnings =
    [
        // Comparison made to same variable; did you mean to compare something else?
        "1718"
    ];

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleDontDisableWarnings,
                                                                               category: Categories.IllegalPragmas,
                                                                               title: "Don't disable warnings with #pragma warning disable",
                                                                               message: "Don't disable warnings using #pragma warning disable");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static bool IsBanned(string code)
    {
        return !AllowedWarnings.Contains(value: code, comparer: StringComparer.Ordinal);
    }

    private static bool IsBannedForTestAssemblies(string code)
    {
        return AllowedInTestWarnings.Contains(value: code, comparer: StringComparer.Ordinal) || IsBanned(code);
    }

    private static Func<string, bool> DetermineWarningList(Compilation compilation)
    {
        return compilation.IsTestAssembly()
            ? IsBannedForTestAssemblies
            : IsBanned;
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        Banned banned = new(DetermineWarningList(compilationStartContext.Compilation));

        compilationStartContext.RegisterSyntaxNodeAction(action: banned.LookForBannedMethods, SyntaxKind.PragmaWarningDirectiveTrivia);
    }

    [DebuggerDisplay("IsBaned = {_isBanned}")]
    private readonly record struct Banned
    {
        private readonly Func<string, bool> _isBanned;

        public Banned(Func<string, bool> isBanned)
        {
            this._isBanned = isBanned;
        }

        [SuppressMessage(category: "Roslynator.Analyzers", checkId: "RCS1231:Make parameter ref read only", Justification = "Needed here")]
        public void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective)
            {
                return;
            }

            foreach (ExpressionSyntax invocation in this.BannedInvocations(pragmaWarningDirective: pragmaWarningDirective))
            {
                invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
            }
        }

        private IEnumerable<ExpressionSyntax> BannedInvocations(PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective)
        {
            Func<string, bool>? isBanned = this._isBanned;

            return pragmaWarningDirective.ErrorCodes.Where(invocation => isBanned(invocation.ToString()));
        }
    }
}