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
        internal const string DiagnosticId = "FFS0001";
        private const string CATEGORY = "Illegal Method Calls";

        private static readonly LocalizableString Description = new LiteralString(value: @"Call DateTime.UtcNow rather than DateTime.Now");

        private static readonly LocalizableString MessageFormat = new LiteralString(value: @"Call DateTime.UtcNow rather than DateTime.Now");

        private static readonly LocalizableString Title = new LiteralString(value: @"Avoid use of DateTime");

        private static readonly DiagnosticDescriptor Rule =
            new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true, Description);

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol dateTimeType = compilationStartContext.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName: "System.DateTime");
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

                                                                             INamedTypeSymbol typeInfo = analysisContext.SemanticModel.GetTypeInfo(e)
                                                                                                                        .Type as INamedTypeSymbol;

                                                                             if (typeInfo?.ConstructedFrom == null)
                                                                             {
                                                                                 continue;
                                                                             }

                                                                             if (!StringComparer.InvariantCultureIgnoreCase.Equals(typeInfo.ConstructedFrom.MetadataName,
                                                                                                                                   dateTimeType.MetadataName))
                                                                             {
                                                                                 continue;
                                                                             }

                                                                             // note cannot use the names as it references itself
                                                                             if (invocation.Name.ToString() == @"Now")
                                                                             {
                                                                                 analysisContext.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation()));
                                                                             }
                                                                         }
                                                                     },
                                                             SyntaxKind.MethodDeclaration);
        }
    }
}