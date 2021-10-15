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
            "1591"
        };

        private static readonly string[] AllowedInTestWarnings =
        {
            // Comparison made to same variable; did you mean to compare something else?
            @"1718"
        };

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleDontDisableWarnings,
                                                                                   category: CATEGORY,
                                                                                   title: "Don't disable warnings with #pragma warning disable",
                                                                                   message: "Don't disable warnings using #pragma warning disable");

        private static readonly IReadOnlyList<string> TestAssemblies = new[] { @"Microsoft.NET.Test.Sdk" };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] { Rule }.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static bool IsBanned(string code)
        {
            return !AllowedWarnings.Contains(code);
        }

        private static bool IsBannedForTestAssemblies(string code)
        {
            return AllowedInTestWarnings.Contains(code) || IsBanned(code);
        }

        private static bool IsTestAssembly(Compilation compilation)
        {
            try
            {
                return compilation.ReferencedAssemblyNames.SelectMany(collectionSelector: _ => TestAssemblies,
                                                                      resultSelector: (assembly, testAssemblyName) => new { assembly, testAssemblyName })
                                  .Where(t => StringComparer.InvariantCultureIgnoreCase.Equals(x: t.assembly.Name, y: t.testAssemblyName))
                                  .Select(t => t.assembly)
                                  .Any();
            }
            catch
            {
                return false;
            }
        }

        private static Func<string, bool> DetermineWarningList(Compilation compilation)
        {
            if (IsTestAssembly(compilation))
            {
                return IsBannedForTestAssemblies;
            }

            return IsBanned;
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            Func<string, bool> isBanned = DetermineWarningList(compilationStartContext.Compilation);

            void LookForBannedMethods(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                if (syntaxNodeAnalysisContext.Node is not PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective)
                {
                    return;
                }

                foreach (ExpressionSyntax invocation in pragmaWarningDirective.ErrorCodes.Where(invocation => isBanned(invocation.ToString())))
                {
                    syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, invocation.GetLocation()));
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.PragmaWarningDirectiveTrivia);
        }
    }
}