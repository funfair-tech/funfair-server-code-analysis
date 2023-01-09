using System;
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
///     Looks for prohibited classes in test assemblies.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedClassesInTestAssembliesDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<ProhibitedClassSpec> BannedClasses = new ProhibitedClassSpec[]
                                                                               {
                                                                                   new(ruleId: Rules.RuleDontUseSystemConsoleInTestProjects,
                                                                                       title: "Avoid use of System.Console class",
                                                                                       message: "Use ITestOutputHelper rather than System.Console in test projects",
                                                                                       sourceClass: "System.Console")
                                                                               };

    private static readonly string[] TestAssemblies =
    {
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.core"
    };

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        BannedClasses.Select(selector: r => r.Rule)
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
        void LookForBannedClasses(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            Dictionary<string, INamedTypeSymbol> cachedSymbols = BuildCachedSymbols(compilationStartContext.Compilation);
            string? typeSymbol = GetNameIfBanned(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, cachedSymbols: cachedSymbols);

            if (typeSymbol == null)
            {
                return;
            }

            ProhibitedClassSpec? bannedClass = BannedClasses.FirstOrDefault(rule => StringComparer.Ordinal.Equals(x: typeSymbol, y: rule.SourceClass));

            if (bannedClass != null)
            {
                ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, bannedClass: bannedClass);
            }
        }

        if (compilationStartContext.Compilation.ReferencedAssemblyNames.Any(IsTestAssembly))
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedClasses, SyntaxKind.InvocationExpression);
        }
    }

    private static void ReportDiagnostics(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ProhibitedClassSpec bannedClass)
    {
        syntaxNodeAnalysisContext.Node.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: bannedClass.Rule);
    }

    private static bool IsTestAssembly(AssemblyIdentity r)
    {
        return TestAssemblies.Any(a => StringComparer.InvariantCultureIgnoreCase.Equals(x: a, y: r.Name));
    }

    private static Dictionary<string, INamedTypeSymbol> BuildCachedSymbols(Compilation compilation)
    {
        Dictionary<string, INamedTypeSymbol> cachedSymbols = new(StringComparer.Ordinal);

        foreach (string ruleSourceClass in BannedClasses.Select(rule => rule.SourceClass))
        {
            if (!cachedSymbols.ContainsKey(ruleSourceClass))
            {
                INamedTypeSymbol? item = compilation.GetTypeByMetadataName(ruleSourceClass);

                if (item != null)
                {
                    cachedSymbols.Add(key: ruleSourceClass, value: item);
                }
            }
        }

        return cachedSymbols;
    }

    private static string? GetNameIfBanned(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Dictionary<string, INamedTypeSymbol> cachedSymbols)
    {
        if (syntaxNodeAnalysisContext.Node is not InvocationExpressionSyntax invocation)
        {
            return null;
        }

        IMethodSymbol? memberSymbol = MethodSymbolHelper.FindInvokedMemberSymbol(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

        if (memberSymbol == null)
        {
            return null;
        }

        ITypeSymbol? receivingType = memberSymbol.ReceiverType;
        string? fullName = receivingType?.ToFullyQualifiedName();

        if (fullName == null)
        {
            return null;
        }

        return cachedSymbols.TryGetValue(key: fullName, out INamedTypeSymbol? _)
            ? fullName
            : null;
    }

    private sealed class ProhibitedClassSpec
    {
        public ProhibitedClassSpec(string ruleId, string title, string message, string sourceClass)
        {
            this.SourceClass = sourceClass;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.IllegalClassUsage, title: title, message: message);
        }

        public string SourceClass { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}