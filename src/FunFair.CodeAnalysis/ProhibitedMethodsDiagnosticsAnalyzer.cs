using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
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
    [DiagnosticAnalyzer(firstLanguage: LanguageNames.CSharp)]
    public sealed class ProhibitedMethodsDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Illegal Method Calls";

        private static readonly ProhibitedMethodsSpec[] BannedMethods =
        {
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseDateTimeNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "Now"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseDateTimeUtcNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "UtcNow"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseDateTimeToday,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "Today"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseDateTimeOffsetNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
                                      sourceClass: "System.DateTimeOffset",
                                      bannedMethod: "Now"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseDateTimeOffsetUtcNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
                                      sourceClass: "System.DateTimeOffset",
                                      bannedMethod: "UtcNow"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseArbitrarySql,
                                      title: @"Avoid use of inline SQL statements",
                                      message: "Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
                                      sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
                                      bannedMethod: "ExecuteArbitrarySqlAsync"),
            new ProhibitedMethodsSpec(ruleId: Rules.RuleDontUseArbitrarySqlForQueries,
                                      title: @"Avoid use of inline SQL statements",
                                      message: "Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
                                      sourceClass: "FunFair.Common.Data.ISqlServerDatabase",
                                      bannedMethod: "QueryArbitrarySqlAsync")
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            BannedMethods.Select(selector: r => r.Rule)
                         .ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(analysisMode: GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(action: PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            Dictionary<string, INamedTypeSymbol> cachedSymbols = new Dictionary<string, INamedTypeSymbol>();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (!cachedSymbols.ContainsKey(key: rule.SourceClass))
                {
                    INamedTypeSymbol item = compilationStartContext.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName: rule.SourceClass);

                    if (item != null)
                    {
                        cachedSymbols.Add(key: rule.SourceClass, value: item);
                    }
                }
            }

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                IEnumerable<MemberAccessExpressionSyntax> invocations = syntaxNodeAnalysisContext.Node.DescendantNodes()
                                                                                                 .OfType<MemberAccessExpressionSyntax>();

                foreach (MemberAccessExpressionSyntax invocation in invocations)
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
                        continue;
                    }

                    INamedTypeSymbol? typeInfo = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(expression: e)
                                                                          .Type as INamedTypeSymbol;

                    if (typeInfo?.ConstructedFrom == null)
                    {
                        continue;
                    }

                    foreach (ProhibitedMethodsSpec item in BannedMethods)
                    {
                        if (cachedSymbols.TryGetValue(key: item.SourceClass, value: out INamedTypeSymbol metadataType))
                        {
                            if (StringComparer.OrdinalIgnoreCase.Equals(x: typeInfo.ConstructedFrom.MetadataName, y: metadataType.MetadataName))
                            {
                                if (invocation.Name.ToString() == item.BannedMethod)
                                {
                                    syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic: Diagnostic.Create(descriptor: item.Rule, location: invocation.GetLocation()));
                                }
                            }
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.ConstructorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.ConversionOperatorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.MethodDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.OperatorDeclaration);
            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.PropertyDeclaration);
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod)
            {
                this.SourceClass = sourceClass;
                this.BannedMethod = bannedMethod;
                this.Rule = CreateRule(code: ruleId, title: title, message: message);
            }

            public string SourceClass { get; }

            public string BannedMethod { get; }

            public DiagnosticDescriptor Rule { get; }

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(value: title);
                LiteralString translatableMessage = new LiteralString(value: message);

                return new DiagnosticDescriptor(id: code,
                                                title: translatableTitle,
                                                messageFormat: translatableMessage,
                                                category: CATEGORY,
                                                defaultSeverity: DiagnosticSeverity.Error,
                                                isEnabledByDefault: true,
                                                description: translatableMessage);
            }
        }
    }
}