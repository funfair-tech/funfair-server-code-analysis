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
    ///     Looks for issues with class declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ClassVisibilityDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Classes";

        private static readonly IReadOnlyList<ConfiguredClass> Classes = new[]
                                                                         {
                                                                             new ConfiguredClass(ruleId: Rules.MockBaseClassInstancesMustBeInternal,
                                                                                                 title: "MockBase<T> instances must be internal",
                                                                                                 message: "MockBase<T> instances must be internal",
                                                                                                 className: "FunFair.Test.Common.Mocks.MockBase<T>",
                                                                                                 visibility: SyntaxKind.InternalKeyword),
                                                                             new ConfiguredClass(ruleId: Rules.MockBaseClassInstancesMustBeSealed,
                                                                                                 title: "MockBase<T> instances must be sealed",
                                                                                                 message: "MockBase<T> instances must be sealed",
                                                                                                 className: "FunFair.Test.Common.Mocks.MockBase<T>",
                                                                                                 visibility: SyntaxKind.SealedKeyword)
                                                                         };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            Classes.Select(c => c.Rule)
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
            compilationStartContext.RegisterSyntaxNodeAction(action: CheckClassVisibility, SyntaxKind.ClassDeclaration);
        }

        private static void CheckClassVisibility(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not ClassDeclarationSyntax classDeclarationSyntax)
            {
                return;
            }

            foreach (ConfiguredClass classDefinition in Classes)
            {
                if (classDefinition.TypeMatchesClass(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext) && !classDefinition.HasCorrectClassModifier(classDeclarationSyntax: classDeclarationSyntax))
                {
                    syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: classDefinition.Rule, classDeclarationSyntax.GetLocation()));
                }
            }
        }

        private sealed class ConfiguredClass
        {
            public ConfiguredClass(string ruleId, string title, string message, string className, SyntaxKind visibility)
            {
                this.ClassName = className;
                this.Visibility = visibility;
                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
            }

            public DiagnosticDescriptor Rule { get; }

            private string ClassName { get; }

            private SyntaxKind Visibility { get; }

            public bool TypeMatchesClass(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
            {
                INamedTypeSymbol? containingType = syntaxNodeAnalysisContext.ContainingSymbol as INamedTypeSymbol;

                if (containingType == null)
                {
                    return false;
                }

                for (INamedTypeSymbol? parent = containingType.BaseType; parent != null; parent = parent.BaseType)
                {
                    INamedTypeSymbol originalDefinition = parent.OriginalDefinition;

                    if (SymbolDisplay.ToDisplayString(originalDefinition) == this.ClassName)
                    {
                        return true;
                    }
                }

                return false;
            }

            public bool HasCorrectClassModifier(ClassDeclarationSyntax classDeclarationSyntax)
            {
                static bool MatchesVisibility(ConfiguredClass classDefinition, SyntaxToken syntaxToken)
                {
                    return syntaxToken.Kind() == classDefinition.Visibility;
                }

                return classDeclarationSyntax.Modifiers.Any(modifier => MatchesVisibility(this, syntaxToken: modifier));
            }
        }
    }
}