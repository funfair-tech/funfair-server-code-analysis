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
    ///     Looks for issues with record declarations
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class DebuggerDisplayAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = Categories.Debugging;

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleRecordsShouldSpecifyDebuggerDisplay,
                                                                                   category: CATEGORY,
                                                                                   title: "Should have DebuggerDisplay attribute",
                                                                                   message: "Should have DebuggerDisplay attribute");

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
            compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.RecordDeclaration);
        }

        private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not RecordDeclarationSyntax recordDeclarationSyntax)
            {
                return;
            }

            if (!recordDeclarationSyntax.AttributeLists.SelectMany(selector: al => al.Attributes)
                                        .Select(attribute => syntaxNodeAnalysisContext.SemanticModel.GetTypeInfo(attribute))
                                        .Select(ti => ti.Type)
                                        .RemoveNulls()
                                        .Any(ti => IsDebuggerDisplayAttribute(ti.ToDisplayString())))

            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, recordDeclarationSyntax.GetLocation()));
            }
        }

        private static bool IsDebuggerDisplayAttribute(string fullName)
        {
            return fullName == "System.Diagnostics.DebuggerDisplayAttribute";
        }
    }
}