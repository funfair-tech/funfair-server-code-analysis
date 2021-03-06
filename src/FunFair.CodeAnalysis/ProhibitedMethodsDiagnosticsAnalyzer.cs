using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <inheritdoc />
    /// <summary>
    ///     Looks for prohibited methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProhibitedMethodsDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Illegal Method Calls";

        private static readonly ProhibitedMethodsSpec[] BannedMethods =
        {
            new(ruleId: Rules.RuleDontUseDateTimeNow,
                title: @"Avoid use of DateTime methods",
                message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                sourceClass: "System.DateTime",
                bannedMethod: "Now"),
            new(ruleId: Rules.RuleDontUseDateTimeUtcNow,
                title: @"Avoid use of DateTime methods",
                message: "Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
                sourceClass: "System.DateTime",
                bannedMethod: "UtcNow"),
            new(ruleId: Rules.RuleDontUseDateTimeToday,
                title: @"Avoid use of DateTime methods",
                message: "Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
                sourceClass: "System.DateTime",
                bannedMethod: "Today"),
            new(ruleId: Rules.RuleDontUseDateTimeOffsetNow,
                title: @"Avoid use of DateTime methods",
                message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
                sourceClass: "System.DateTimeOffset",
                bannedMethod: "Now"),
            new(ruleId: Rules.RuleDontUseDateTimeOffsetUtcNow,
                title: @"Avoid use of DateTime methods",
                message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
                sourceClass: "System.DateTimeOffset",
                bannedMethod: "UtcNow"),
            new(ruleId: Rules.RuleDontUseArbitrarySql,
                title: @"Avoid use of inline SQL statements",
                message: "Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
                sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
                bannedMethod: "ExecuteArbitrarySqlAsync"),
            new(ruleId: Rules.RuleDontUseArbitrarySqlForQueries,
                title: @"Avoid use of inline SQL statements",
                message: "Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
                sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
                bannedMethod: "QueryArbitrarySqlAsync"),
            new(ruleId: Rules.RuleDontReadRemoteIpAddressDirectlyFromConnection,
                title: @"Use RemoteIpAddressRetriever instead of getting RemoteIpAddress directly from the HttpRequest",
                message: "Use RemoteIpAddressRetriever",
                sourceClass: "Microsoft.AspNetCore.Http.ConnectionInfo",
                bannedMethod: "RemoteIpAddress")
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            BannedMethods.Select(selector: r => r.Rule)
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
            Dictionary<string, INamedTypeSymbol> cachedSymbols = BuildCachedSymbols(compilationStartContext.Compilation);

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                IEnumerable<MemberAccessExpressionSyntax> invocations = syntaxNodeAnalysisContext.Node.DescendantNodes()
                                                                                                 .OfType<MemberAccessExpressionSyntax>();

                foreach (MemberAccessExpressionSyntax invocation in invocations)
                {
                    INamedTypeSymbol? typeInfo = ExtractExpressionSyntax(invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);

                    if (typeInfo == null)
                    {
                        continue;
                    }

                    ReportAnyBannedSymbols(cachedSymbols: cachedSymbols, typeInfo: typeInfo, invocation: invocation, syntaxNodeAnalysisContext: syntaxNodeAnalysisContext);
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.ConstructorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.ConversionOperatorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.MethodDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.OperatorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.PropertyDeclaration);
        }

        private static void ReportAnyBannedSymbols(Dictionary<string, INamedTypeSymbol> cachedSymbols,
                                                   INamedTypeSymbol typeInfo,
                                                   MemberAccessExpressionSyntax invocation,
                                                   SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            foreach (ProhibitedMethodsSpec item in BannedMethods)
            {
                if (cachedSymbols.TryGetValue(key: item.SourceClass, out INamedTypeSymbol metadataType))
                {
                    if (StringComparer.OrdinalIgnoreCase.Equals(x: typeInfo.ConstructedFrom.MetadataName, y: metadataType.MetadataName))
                    {
                        if (invocation.Name.Identifier.ToString() == item.BannedMethod)
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: item.Rule, invocation.GetLocation()));
                        }
                    }
                }
            }
        }

        private static Dictionary<string, INamedTypeSymbol> BuildCachedSymbols(Compilation compilation)
        {
            Dictionary<string, INamedTypeSymbol> cachedSymbols = new();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (!cachedSymbols.ContainsKey(rule.SourceClass))
                {
                    INamedTypeSymbol? item = compilation.GetTypeByMetadataName(rule.SourceClass);

                    if (item != null)
                    {
                        cachedSymbols.Add(key: rule.SourceClass, value: item);
                    }
                }
            }

            return cachedSymbols;
        }

        private static INamedTypeSymbol? ExtractExpressionSyntax(MemberAccessExpressionSyntax invocation, SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            ExpressionSyntax e;

            if (invocation.Expression is MemberAccessExpressionSyntax syntax)
            {
                e = syntax;
            }
            else if (invocation.Expression is IdentifierNameSyntax expression)
            {
                e = expression;
            }
            else
            {
                return null;
            }

            INamedTypeSymbol? typeInfo = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(e)
                                                                  .Type as INamedTypeSymbol;

            if (typeInfo?.ConstructedFrom == null)
            {
                return null;
            }

            return typeInfo;
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod)
            {
                this.SourceClass = sourceClass;
                this.BannedMethod = bannedMethod;
                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
            }

            public string SourceClass { get; }

            public string BannedMethod { get; }

            public DiagnosticDescriptor Rule { get; }
        }
    }
}