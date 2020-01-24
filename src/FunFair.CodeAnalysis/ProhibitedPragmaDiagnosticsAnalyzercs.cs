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
    public sealed class ProhibitedPragmasDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Illegal Pragmas";

        private static readonly string[] AllowedWarnings =
        {
            // Xml Docs
            "1591",

            // Nullable Reference types - TODO: FIX THESE
            "8600",

            // Nullable Reference types - TODO: FIX THESE
            "8601",

            // Nullable Reference types - TODO: FIX THESE
            "8602",

            // Nullable Reference types - TODO: FIX THESE
            "8603",

            // Nullable Reference types - TODO: FIX THESE
            "8604",

            // Nullable Reference types - TODO: FIX THESE
            "8618",

            // Nullable Reference types - TODO: FIX THESE
            "8619",

            // Nullable Reference types - TODO: FIX THESE
            "8620",

            // Nullable Reference types - TODO: FIX THESE
            "8622",

            // Nullable Reference types - TODO: FIX THESE
            "8625",

            // Nullable Reference types - TODO: FIX THESE
            "8653"
        };

        private static readonly string[] AllowedInTestWarnings =
        {
            // Comparison made to same variable; did you mean to compare something else?
            @"1718"
        };

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(Rules.RuleDontDisableWarnings,
                                                                                   CATEGORY,
                                                                                   title: "Don't disable warnings with #pragma warning disable",
                                                                                   message: "Don't disable warnings using #pragma warning disable");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            bool isTestAssembly = compilationStartContext.Compilation.ReferencedAssemblyNames.Any(predicate: name => name.Name == @"Microsoft.NET.Test.Sdk");

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                if (syntaxNodeAnalysisContext.Node is PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective)
                {
                    foreach (ExpressionSyntax invocation in pragmaWarningDirective.ErrorCodes)
                    {
                        if (isTestAssembly && AllowedInTestWarnings.Contains(invocation.ToString()))
                        {
                            continue;
                        }

                        if (!AllowedWarnings.Contains(invocation.ToString()))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
                        }
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(LookForBannedMethods, SyntaxKind.PragmaWarningDirectiveTrivia);
        }
    }
}