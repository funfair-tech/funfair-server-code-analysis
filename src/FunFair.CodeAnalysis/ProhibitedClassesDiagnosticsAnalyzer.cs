using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <inheritdoc />
/// <summary>
///     Looks for prohibited classes.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedClassesDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<ProhibitedClassSpec> BannedClasses = new ProhibitedClassSpec[]
                                                                               {
                                                                                   new(ruleId: Rules.RuleDontUseConcurrentDictionary,
                                                                                       title: "Avoid use of System.Collections.Concurrent.ConcurrentDictionary class",
                                                                                       message: "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                                                                                       sourceClass: "System.Collections.Concurrent.ConcurrentDictionary`2")
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
            IEnumerable<INamedTypeSymbol>? typeSymbols = LookForUsageOfBannedClasses(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, cachedSymbols: cachedSymbols);

            if (typeSymbols is null)
            {
                return;
            }

            ReportAnyBannedSymbols(typeSymbols.ToList(), syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);
        }

        compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedClasses,
                                                         SyntaxKind.ObjectCreationExpression,
                                                         SyntaxKind.FieldDeclaration,
                                                         SyntaxKind.VariableDeclarator,
                                                         SyntaxKind.MethodDeclaration,
                                                         SyntaxKind.PropertyDeclaration,
                                                         SyntaxKind.ConstructorDeclaration);
    }

    private static void ReportAnyBannedSymbols(IReadOnlyCollection<INamedTypeSymbol> typeSymbols, in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        foreach (INamedTypeSymbol typeSymbol in typeSymbols)
        {
            ProhibitedClassSpec? bannedClass = BannedClasses.FirstOrDefault(rule => StringComparer.OrdinalIgnoreCase.Equals(typeSymbol.ToFullyQualifiedName(), y: rule.SourceClass));

            if (bannedClass is not null)
            {
                syntaxNodeAnalysisContext.Node.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: bannedClass.Rule);
            }
        }
    }

    private static Dictionary<string, INamedTypeSymbol> BuildCachedSymbols(Compilation compilation)
    {
        Dictionary<string, INamedTypeSymbol> cachedSymbols = new(StringComparer.Ordinal);

        foreach (string ruleSourceClass in BannedClasses.Select(rule => rule.SourceClass))
        {
            if (!cachedSymbols.ContainsKey(ruleSourceClass))
            {
                INamedTypeSymbol? item = compilation.GetTypeByMetadataName(ruleSourceClass);

                if (item is not null)
                {
                    cachedSymbols.Add(key: ruleSourceClass, value: item);
                }
            }
        }

        return cachedSymbols;
    }

    private static IEnumerable<INamedTypeSymbol>? LookForUsageOfBannedClasses(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Dictionary<string, INamedTypeSymbol> cachedSymbols)
    {
        ISymbol? symbol = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(declaration: syntaxNodeAnalysisContext.Node, cancellationToken: syntaxNodeAnalysisContext.CancellationToken);

        if (symbol is null)
        {
            return LookupSymbolInContext(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, cachedSymbols: cachedSymbols);
        }

        return symbol.OriginalDefinition switch
        {
            IPropertySymbol propertySymbol => GetSymbol(new[]
                                                        {
                                                            propertySymbol.Type
                                                        },
                                                        cachedSymbols: cachedSymbols),
            IFieldSymbol fieldSymbol => GetSymbol(new[]
                                                  {
                                                      fieldSymbol.Type
                                                  },
                                                  cachedSymbols: cachedSymbols),
            IMethodSymbol parameterSymbol => GetSymbol(parameterSymbol.Parameters.Select(x => x.Type), cachedSymbols: cachedSymbols),
            _ => LookupSymbolInContext(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, cachedSymbols: cachedSymbols)
        };
    }

    private static IEnumerable<INamedTypeSymbol>? LookupSymbolInContext(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, Dictionary<string, INamedTypeSymbol> cachedSymbols)
    {
        ITypeSymbol? typeInfo = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(node: syntaxNodeAnalysisContext.Node, cancellationToken: syntaxNodeAnalysisContext.CancellationToken)
                                                         .Type;

        if (typeInfo is not null)
        {
            return GetSymbol(new[]
                             {
                                 typeInfo
                             },
                             cachedSymbols: cachedSymbols);
        }

        return null;
    }

    private static IEnumerable<INamedTypeSymbol> GetSymbol(IEnumerable<ITypeSymbol> symbols, Dictionary<string, INamedTypeSymbol> cachedSymbols)
    {
        return symbols.Select(symbol => GetSymbol(typeSymbol: symbol, cachedSymbols: cachedSymbols))
                      .Where(symbol => symbol is not null)!;
    }

    /// <summary>
    ///     Get symbol from list of defined banned classes
    /// </summary>
    /// <param name="typeSymbol">Type symbol to get</param>
    /// <param name="cachedSymbols">The symbol cache</param>
    /// <returns></returns>
    private static INamedTypeSymbol? GetSymbol(ITypeSymbol typeSymbol, Dictionary<string, INamedTypeSymbol> cachedSymbols)
    {
        string? fullyQualifiedSymbolName = typeSymbol.ToFullyQualifiedName();

        if (fullyQualifiedSymbolName is null)
        {
            return null;
        }

        return cachedSymbols.TryGetValue(key: fullyQualifiedSymbolName, out INamedTypeSymbol? symbol)
            ? symbol
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