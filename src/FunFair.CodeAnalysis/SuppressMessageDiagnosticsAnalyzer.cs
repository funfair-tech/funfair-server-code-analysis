using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for problems with <see cref="SuppressMessageAttribute" />
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class SuppressMessageDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = Categories.SuppressedErrors;

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleSuppressMessageMustHaveJustification,
                                                                                   category: CATEGORY,
                                                                                   title: "SuppressMessage must specify a Justification",
                                                                                   message: "SuppressMessage must specify a Justification");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            new[]
            {
                Rule
            }.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol? sourceClassType = compilationStartContext.Compilation.GetTypeByMetadataName(typeof(SuppressMessageAttribute).FullName);

            if (sourceClassType == null)
            {
                return;
            }

            compilationStartContext.RegisterSyntaxNodeAction(action: syntaxNodeAnalysisContext =>
                                                                         MustDeriveFromTestBase(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                                                                sourceClassType: sourceClassType),
                                                             SyntaxKind.Attribute);
        }

        private static void MustDeriveFromTestBase(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, INamedTypeSymbol sourceClassType)
        {
            if (syntaxNodeAnalysisContext.Node is not AttributeSyntax methodDeclarationSyntax)
            {
                return;
            }

            TypeInfo ti = syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(methodDeclarationSyntax.Name);

            if (ti.Type?.MetadataName != sourceClassType.MetadataName)
            {
                return;
            }

            AttributeArgumentSyntax? justification = methodDeclarationSyntax.ArgumentList?.Arguments.FirstOrDefault(k => k.NameEquals?.Name.Identifier.Text == "Justification");

            if (justification == null)
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, methodDeclarationSyntax.GetLocation()));

                return;
            }

            if (justification.Expression is LiteralExpressionSyntax l)
            {
                string text = l.Token.ValueText;

                if (string.IsNullOrWhiteSpace(text))
                {
                    syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, l.GetLocation()));
                }
            }
        }
    }
}