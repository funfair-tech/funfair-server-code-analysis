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
    /// <summary>
    ///     Looks for prohibited methods.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ProhibitedMethodsDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Illegal Method Calls";

        private static readonly ProhibitedMethodsSpec[] BannedMethods =
        {
            new ProhibitedMethodsSpec(Rules.RuleDontUseDateTimeNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "Now"),
            new ProhibitedMethodsSpec(Rules.RuleDontUseDateTimeUtcNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "UtcNow"),
            new ProhibitedMethodsSpec(Rules.RuleDontUseDateTimeToday,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
                                      sourceClass: "System.DateTime",
                                      bannedMethod: "Today"),
            new ProhibitedMethodsSpec(Rules.RuleDontUseDateTimeOffsetNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
                                      sourceClass: "System.DateTimeOffset",
                                      bannedMethod: "Now"),
            new ProhibitedMethodsSpec(Rules.RuleDontUseDateTimeOffsetUtcNow,
                                      title: @"Avoid use of DateTime methods",
                                      message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
                                      sourceClass: "System.DateTimeOffset",
                                      bannedMethod: "UtcNow")
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
            Dictionary<string, INamedTypeSymbol> cachedSymbols = new Dictionary<string, INamedTypeSymbol>();

            foreach (ProhibitedMethodsSpec rule in BannedMethods)
            {
                if (!cachedSymbols.ContainsKey(rule.SourceClass))
                {
                    INamedTypeSymbol item = compilationStartContext.Compilation.GetTypeByMetadataName(rule.SourceClass);

                    cachedSymbols.Add(rule.SourceClass, item);
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: analysisContext =>
                                                                     {
                                                                         IEnumerable<MemberAccessExpressionSyntax> invocations = analysisContext.Node.DescendantNodes()
                                                                                                                                                .OfType<
                                                                                                                                                    MemberAccessExpressionSyntax
                                                                                                                                                >();

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

                                                                             INamedTypeSymbol? typeInfo = analysisContext.SemanticModel.GetTypeInfo(e)
                                                                                                                         .Type as INamedTypeSymbol;

                                                                             if (typeInfo?.ConstructedFrom == null)
                                                                             {
                                                                                 continue;
                                                                             }

                                                                             foreach (ProhibitedMethodsSpec item in BannedMethods)
                                                                             {
                                                                                 if (cachedSymbols.TryGetValue(item.SourceClass, out INamedTypeSymbol metadataType))
                                                                                 {
                                                                                     if (StringComparer.OrdinalIgnoreCase.Equals(typeInfo.ConstructedFrom.MetadataName,
                                                                                                                                 metadataType.MetadataName))
                                                                                     {
                                                                                         if (invocation.Name.ToString() == item.BannedMethod)
                                                                                         {
                                                                                             analysisContext.ReportDiagnostic(
                                                                                                 Diagnostic.Create(item.Rule, invocation.GetLocation()));
                                                                                         }
                                                                                     }
                                                                                 }
                                                                             }
                                                                         }
                                                                     },
                                                             SyntaxKind.MethodDeclaration);
        }

        private sealed class ProhibitedMethodsSpec
        {
            public ProhibitedMethodsSpec(string ruleId, string title, string message, string sourceClass, string bannedMethod)
            {
                this.SourceClass = sourceClass;
                this.BannedMethod = bannedMethod;
                this.Rule = CreateRule(ruleId, title, message);
            }

            public string SourceClass { get; }

            public string BannedMethod { get; }

            public DiagnosticDescriptor Rule { get; }

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(title);
                LiteralString translatableMessage = new LiteralString(message);

                return new DiagnosticDescriptor(code, translatableTitle, translatableMessage, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true, translatableMessage);
            }
        }
    }
}