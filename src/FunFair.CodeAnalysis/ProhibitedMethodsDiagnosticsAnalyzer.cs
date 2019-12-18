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

        private static readonly DiagnosticDescriptor RuleDontUseDateTimeNow = CreateRule(code: @"FFS0001",
                                                                                         title: @"Avoid use of DateTime methods",
                                                                                         message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now");

        private static readonly DiagnosticDescriptor RuleDontUseDateTimeOffsetNow = CreateRule(code: @"FFS0002",
                                                                                               title: @"Avoid use of DateTimeOffset methods",
                                                                                               message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleDontUseDateTimeNow, RuleDontUseDateTimeOffsetNow);

        private static DiagnosticDescriptor CreateRule(string code, string title, string message)
        {
            LiteralString translatableTitle = new LiteralString(title);
            LiteralString translatableMessage = new LiteralString(message);

            return new DiagnosticDescriptor(code, translatableTitle, translatableMessage, CATEGORY, DiagnosticSeverity.Error, isEnabledByDefault: true, translatableMessage);
        }

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            INamedTypeSymbol dateTimeType = compilationStartContext.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName: "System.DateTime");
            INamedTypeSymbol dateTimeOffsetType = compilationStartContext.Compilation.GetTypeByMetadataName(fullyQualifiedMetadataName: "System.DateTimeOffset");

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

                                                                             if (StringComparer.OrdinalIgnoreCase.Equals(typeInfo.ConstructedFrom.MetadataName,
                                                                                                                         dateTimeType.MetadataName))
                                                                             {
                                                                                 if (invocation.Name.ToString() == @"Now")
                                                                                 {
                                                                                     analysisContext.ReportDiagnostic(
                                                                                         Diagnostic.Create(RuleDontUseDateTimeNow, invocation.GetLocation()));
                                                                                 }
                                                                             }

                                                                             if (StringComparer.OrdinalIgnoreCase.Equals(typeInfo.ConstructedFrom.MetadataName,
                                                                                                                         dateTimeOffsetType.MetadataName))
                                                                             {
                                                                                 if (invocation.Name.ToString() == @"Now")
                                                                                 {
                                                                                     analysisContext.ReportDiagnostic(
                                                                                         Diagnostic.Create(RuleDontUseDateTimeOffsetNow, invocation.GetLocation()));
                                                                                 }
                                                                             }
                                                                         }
                                                                     },
                                                             SyntaxKind.MethodDeclaration);
        }
    }
}