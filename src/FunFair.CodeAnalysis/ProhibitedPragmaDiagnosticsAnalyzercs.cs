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

        private static readonly IReadOnlyList<string> TestAssemblies = new[] {@"Microsoft.NET.Test.Sdk"};

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] {Rule}.ToImmutableArray();

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
                if (compilation.ReferencedAssemblyNames == null)
                {
                    return false;
                }

                foreach (AssemblyIdentity assembly in compilation.ReferencedAssemblyNames)
                {
                    foreach (string testAssemblyName in TestAssemblies)
                    {
                        if (StringComparer.InvariantCultureIgnoreCase.Equals(x: assembly.Name, y: testAssemblyName))
                        {
                            return true;
                        }
                    }
                }

                return false;
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
                if (!(syntaxNodeAnalysisContext.Node is PragmaWarningDirectiveTriviaSyntax pragmaWarningDirective))
                {
                    return;
                }

                foreach (ExpressionSyntax invocation in pragmaWarningDirective.ErrorCodes)
                {
                    if (isBanned(invocation.ToString()))
                    {
                        syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, invocation.GetLocation()));
                    }
                }
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: LookForBannedMethods, SyntaxKind.PragmaWarningDirectiveTrivia);
        }
    }
}