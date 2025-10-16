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
public sealed class ProhibitedMethodsDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly ProhibitedMethodsSpec[] BannedMethods =
    [
        Build(
            ruleId: Rules.RuleDontUseDateTimeNow,
            title: "Avoid use of DateTime methods",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            sourceClass: "System.DateTime",
            bannedMethod: "Now"
        ),
        Build(
            ruleId: Rules.RuleDontUseDateTimeUtcNow,
            title: "Avoid use of DateTime methods",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
            sourceClass: "System.DateTime",
            bannedMethod: "UtcNow"
        ),
        Build(
            ruleId: Rules.RuleDontUseDateTimeToday,
            title: "Avoid use of DateTime methods",
            message: "Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
            sourceClass: "System.DateTime",
            bannedMethod: "Today"
        ),
        Build(
            ruleId: Rules.RuleDontUseDateTimeOffsetNow,
            title: "Avoid use of DateTime methods",
            message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
            sourceClass: "System.DateTimeOffset",
            bannedMethod: "Now"
        ),
        Build(
            ruleId: Rules.RuleDontUseDateTimeOffsetUtcNow,
            title: "Avoid use of DateTime methods",
            message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
            sourceClass: "System.DateTimeOffset",
            bannedMethod: "UtcNow"
        ),
        Build(
            ruleId: Rules.RuleDontUseArbitrarySql,
            title: "Avoid use of inline SQL statements",
            message: "Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
            sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
            bannedMethod: "ExecuteArbitrarySqlAsync"
        ),
        Build(
            ruleId: Rules.RuleDontUseArbitrarySqlForQueries,
            title: "Avoid use of inline SQL statements",
            message: "Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
            sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
            bannedMethod: "QueryArbitrarySqlAsync"
        ),
        Build(
            ruleId: Rules.RuleDontReadRemoteIpAddressDirectlyFromConnection,
            title: "Use RemoteIpAddressRetriever instead of getting RemoteIpAddress directly from the HttpRequest",
            message: "Use RemoteIpAddressRetriever",
            sourceClass: "Microsoft.AspNetCore.Http.ConnectionInfo",
            bannedMethod: "RemoteIpAddress"
        ),
        Build(
            ruleId: Rules.RuleDontUseGuidParse,
            title: "Use new Guid() with constant guids or Guid.TryParse everywhere else",
            message: "Use new Guid() with constant guids or Guid.TryParse everywhere else",
            sourceClass: "System.Guid",
            bannedMethod: "Parse"
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparerInvariantCulture,
            title: "Use System.StringComparer.Ordinal instead",
            message: "Use System.StringComparer.Ordinal instead",
            sourceClass: "System.StringComparer",
            bannedMethod: nameof(StringComparer.InvariantCulture)
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparerInvariantCultureIgnoreCase,
            title: "Use System.StringComparer.OrdinalIgnoreCase instead",
            message: "Use System.StringComparer.OrdinalIgnoreCase instead",
            sourceClass: "System.StringComparer",
            bannedMethod: nameof(StringComparer.InvariantCultureIgnoreCase)
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparisonInvariantCulture,
            title: "Use System.StringComparison.Ordinal instead",
            message: "Use System.StringComparison.Ordinal instead",
            sourceClass: "System.StringComparison",
            bannedMethod: nameof(StringComparison.InvariantCulture)
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparisonInvariantCultureIgnoreCase,
            title: "Use System.StringComparison.OrdinalIgnoreCase instead",
            message: "Use System.StringComparison.OrdinalIgnoreCase instead",
            sourceClass: "System.StringComparison",
            bannedMethod: nameof(StringComparison.InvariantCultureIgnoreCase)
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparisonCurrentCulture,
            title: "Use System.StringComparison.Ordinal instead",
            message: "Use System.StringComparison.Ordinal instead",
            sourceClass: "System.StringComparison",
            bannedMethod: nameof(StringComparison.CurrentCulture)
        ),
        Build(
            ruleId: Rules.RuleDontUseStringComparisonCurrentCultureIgnoreCase,
            title: "Use System.StringComparison.OrdinalIgnoreCase instead",
            message: "Use System.StringComparison.OrdinalIgnoreCase instead",
            sourceClass: "System.StringComparison",
            bannedMethod: nameof(StringComparison.CurrentCultureIgnoreCase)
        ),
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [.. BannedMethods.Select(selector: r => r.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        Checker checker = new();
        context.RegisterCompilationStartAction(checker.PerformCheck);
    }

    private static ProhibitedMethodsSpec Build(
        string ruleId,
        string title,
        string message,
        string sourceClass,
        string bannedMethod
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            sourceClass: sourceClass,
            bannedMethod: bannedMethod
        );
    }

    private sealed class Checker
    {
        private Dictionary<string, INamedTypeSymbol>? _cachedSymbols;

        public void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(
                action: syntaxNodeAnalysisContext =>
                    this.LookForBannedMethods(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        compilation: compilationStartContext.Compilation
                    ),
                SyntaxKind.PointerMemberAccessExpression,
                SyntaxKind.SimpleMemberAccessExpression
            );
        }

        private void LookForBannedMethods(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            Compilation compilation
        )
        {
            if (syntaxNodeAnalysisContext.Node is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                return;
            }

            INamedTypeSymbol? typeInfo = ExtractTypeInfo(
                memberAccessExpressionSyntax: memberAccessExpressionSyntax,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
            );

            if (typeInfo is null)
            {
                return;
            }

            this.ReportAnyBannedSymbols(
                typeInfo: typeInfo,
                invocation: memberAccessExpressionSyntax,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                compilation: compilation
            );
        }

        private void ReportAnyBannedSymbols(
            INamedTypeSymbol typeInfo,
            MemberAccessExpressionSyntax invocation,
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            Compilation compilation
        )
        {
            Dictionary<string, INamedTypeSymbol> cachedSymbols = this.GetCachedSymbols(compilation);
            string invokedMethodName = invocation.Name.Identifier.ToString();
            string typeMetadataName = typeInfo.ConstructedFrom.MetadataName;

            ProhibitedMethodsSpec? matchedRule = BannedMethods.FirstOrNull(item =>
                IsBannedMethod(
                    item: item,
                    cachedSymbols: cachedSymbols,
                    typeMetadataName: typeMetadataName,
                    invokedMethodName: invokedMethodName
                )
            );

            if (matchedRule is not null)
            {
                invocation.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: matchedRule.Value.Rule
                );
            }
        }

        private static bool IsBannedMethod(
            in ProhibitedMethodsSpec item,
            Dictionary<string, INamedTypeSymbol> cachedSymbols,
            string typeMetadataName,
            string invokedMethodName
        )
        {
            return cachedSymbols.TryGetValue(key: item.SourceClass, out INamedTypeSymbol? metadataType)
                && StringComparer.OrdinalIgnoreCase.Equals(x: typeMetadataName, y: metadataType.MetadataName)
                && StringComparer.Ordinal.Equals(x: invokedMethodName, y: item.BannedMethod);
        }

        private Dictionary<string, INamedTypeSymbol> GetCachedSymbols(Compilation compilation)
        {
            return this._cachedSymbols ??= BuildCachedSymbols(compilation);
        }

        private static Dictionary<string, INamedTypeSymbol> BuildCachedSymbols(Compilation compilation)
        {
            // ! Symbol is never null at this point
            return BannedMethods
                .Select(rule => rule.SourceClass)
                .Distinct(StringComparer.Ordinal)
                .Select(sourceClass =>
                    (SourceClass: sourceClass, Symbol: compilation.GetTypeByMetadataName(sourceClass))
                )
                .Where(tuple => tuple.Symbol is not null)
                .ToDictionary(
                    keySelector: tuple => tuple.SourceClass,
                    elementSelector: tuple => tuple.Symbol!,
                    comparer: StringComparer.Ordinal
                );
        }

        private static INamedTypeSymbol? ExtractTypeInfo(
            MemberAccessExpressionSyntax memberAccessExpressionSyntax,
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext
        )
        {
            ExpressionSyntax? expression = memberAccessExpressionSyntax.Expression switch
            {
                MemberAccessExpressionSyntax syntax => syntax,
                IdentifierNameSyntax identifierName => identifierName,
                _ => null,
            };

            if (expression is null)
            {
                return null;
            }

            INamedTypeSymbol? typeInfo =
                syntaxNodeAnalysisContext
                    .SemanticModel.GetTypeInfo(
                        expression: expression,
                        cancellationToken: syntaxNodeAnalysisContext.CancellationToken
                    )
                    .Type as INamedTypeSymbol;

            return typeInfo?.ConstructedFrom is not null ? typeInfo : null;
        }
    }

    [DebuggerDisplay("{Rule.Id} {Rule.Title} Class {SourceClass} Banned Method: {BannedMethod}")]
    private readonly record struct ProhibitedMethodsSpec
    {
        public ProhibitedMethodsSpec(
            string ruleId,
            string title,
            string message,
            string sourceClass,
            string bannedMethod
        )
        {
            this.SourceClass = sourceClass;
            this.BannedMethod = bannedMethod;
            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.IllegalMethodCalls,
                title: title,
                message: message
            );
        }

        public string SourceClass { get; }

        public string BannedMethod { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}
