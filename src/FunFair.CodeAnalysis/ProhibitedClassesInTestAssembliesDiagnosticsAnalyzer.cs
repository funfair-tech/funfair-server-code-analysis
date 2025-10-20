using System;
using System.Collections.Concurrent;
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
public sealed class ProhibitedClassesInTestAssembliesDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<ProhibitedClassSpec> BannedClasses =
    [
        Build(
            ruleId: Rules.RuleDontUseSystemConsoleInTestProjects,
            title: "Avoid use of System.Console class",
            message: "Use ITestOutputHelper rather than System.Console in test projects",
            sourceClass: "System.Console"
        ),
    ];

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
    [
        .. BannedClasses.Select(r => r.Rule),
    ];

    private static readonly StringComparer ClassNameComparer = StringComparer.Ordinal;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();

        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private static ProhibitedClassSpec Build(string ruleId, string title, string message, string sourceClass)
    {
        return new(ruleId: ruleId, title: title, message: message, sourceClass: sourceClass);
    }

    private sealed class Checker
    {
        private Dictionary<string, INamedTypeSymbol>? _cachedSymbols;
        private readonly ConcurrentDictionary<string, ProhibitedClassSpec?> _specCache = new(ClassNameComparer);

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            if (compilationStartContext.Compilation.IsUnitTestAssembly())
            {
                compilationStartContext.RegisterSyntaxNodeAction(
                    action: syntaxNodeAnalysisContext =>
                        this.LookForBannedClasses(
                            syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                            compilation: compilationStartContext.Compilation
                        ),
                    SyntaxKind.InvocationExpression
                );
            }
        }

        private void LookForBannedClasses(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            Compilation compilation
        )
        {
            Dictionary<string, INamedTypeSymbol> cachedSymbols = this.LookupCachedSymbols(compilation);
            string? typeSymbol = GetNameIfBanned(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                cachedSymbols: cachedSymbols
            );

            if (typeSymbol is null)
            {
                return;
            }

            ProhibitedClassSpec? bannedClass = this.GetBannedClass(typeSymbol);

            if (bannedClass is not null)
            {
                syntaxNodeAnalysisContext.Node.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: bannedClass.Value.Rule
                );
            }
        }

        private ProhibitedClassSpec? GetBannedClass(string typeSymbol)
        {
            if (this._specCache.TryGetValue(key: typeSymbol, out ProhibitedClassSpec? cachedSpec))
            {
                return cachedSpec;
            }

            ProhibitedClassSpec? spec = BannedClasses.FirstOrNull(rule =>
                ClassNameComparer.Equals(x: typeSymbol, y: rule.SourceClass)
            );

            this._specCache[typeSymbol] = spec;
            return spec;
        }

        private Dictionary<string, INamedTypeSymbol> LookupCachedSymbols(Compilation compilation)
        {
            return this._cachedSymbols ??= BuildCachedSymbols(compilation);
        }

        private static Dictionary<string, INamedTypeSymbol> BuildCachedSymbols(Compilation compilation)
        {
            // ! rule item is guaranteed to not be null at the point of access as already filtered
            return BannedClasses
                .Select(rule => rule.SourceClass)
                .Distinct(ClassNameComparer)
                .Select(ruleSourceClass => (ruleSourceClass, item: compilation.GetTypeByMetadataName(ruleSourceClass)))
                .Where(rule => rule.item is not null)
                .ToDictionary(rule => rule.ruleSourceClass, rule => rule.item!, ClassNameComparer);
        }

        private static string? GetNameIfBanned(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            Dictionary<string, INamedTypeSymbol> cachedSymbols
        )
        {
            if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax invocation)
            {
                return null;
            }

            IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(
                invocation: invocation,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
            );

            if (memberSymbol is null)
            {
                return null;
            }

            ITypeSymbol? receivingType = memberSymbol.ReceiverType;
            string? fullName = receivingType?.ToFullyQualifiedName();

            if (fullName is null)
            {
                return null;
            }

            return cachedSymbols.ContainsKey(fullName) ? fullName : null;
        }
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {SourceClass}")]
    private readonly record struct ProhibitedClassSpec
    {
        public ProhibitedClassSpec(string ruleId, string title, string message, string sourceClass)
        {
            this.SourceClass = sourceClass;
            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.IllegalClassUsage,
                title: title,
                message: message
            );
        }

        public string SourceClass { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}
