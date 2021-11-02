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
    ///     Looks for problems with structs.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class OneTypePerDocumentAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Structs";

        private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleOnlyOneTypeDefinedPerFile,
                                                                                   category: CATEGORY,
                                                                                   title: "Should be only one type per file",
                                                                                   message: "Should be only one type per file");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => new[] { Rule }.ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: MustBeReadOnly, SyntaxKind.CompilationUnit);
        }

        private static void MustBeReadOnly(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not CompilationUnitSyntax compilationUnitSyntax)
            {
                return;
            }

            IReadOnlyList<MemberDeclarationSyntax> members = GetNonNestedTypeDeclarations(compilationUnitSyntax)
                .ToArray();

            IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> grouped = members.GroupBy(GetTypeName)
                                                                                       .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                                                                                       .ToArray();

            if (grouped.Count > 1)
            {
                foreach (MemberDeclarationSyntax member in members)
                {
                    syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: Rule, member.GetLocation()));
                }
            }
        }

        private static string GetTypeName(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            return memberDeclarationSyntax switch
            {
                ClassDeclarationSyntax classDeclarationSyntax => NormaliseClass(classDeclarationSyntax),
                RecordDeclarationSyntax recordDeclarationSyntax => NormaliseStruct(recordDeclarationSyntax),
                StructDeclarationSyntax structDeclarationSyntax => NormaliseRecord(structDeclarationSyntax),
                InterfaceDeclarationSyntax interfaceDeclarationSyntax => NormaliseInterface(interfaceDeclarationSyntax),
                EnumDeclarationSyntax enumDeclarationSyntax => NormaliseEnum(enumDeclarationSyntax),
                _ => string.Empty
            };
        }

        private static string NormaliseEnum(EnumDeclarationSyntax enumDeclarationSyntax)
        {
            return string.Concat(str0: "enum:", enumDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseInterface(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
        {
            return string.Concat(str0: "interface:", interfaceDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseRecord(StructDeclarationSyntax structDeclarationSyntax)
        {
            return string.Concat(str0: "struct:", structDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseStruct(RecordDeclarationSyntax recordDeclarationSyntax)
        {
            return string.Concat(str0: "record:", recordDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseClass(ClassDeclarationSyntax classDeclarationSyntax)
        {
            return string.Concat(str0: "class:", classDeclarationSyntax.Identifier.ToString());
        }

        private static IEnumerable<MemberDeclarationSyntax> GetNonNestedTypeDeclarations(CompilationUnitSyntax compilationUnit)
        {
            return GetNonNestedTypeDeclarations(compilationUnit.Members);
        }

        private static IEnumerable<MemberDeclarationSyntax> GetNonNestedTypeDeclarations(SyntaxList<MemberDeclarationSyntax> members)
        {
            foreach (MemberDeclarationSyntax member in members)
            {
                SyntaxKind kind = member.Kind();

                if (kind == SyntaxKind.NamespaceDeclaration)
                {
                    NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)member;

                    foreach (MemberDeclarationSyntax member2 in GetNonNestedTypeDeclarations(namespaceDeclaration.Members))
                    {
                        yield return member2;
                    }
                }
                else if (SyntaxFacts.IsTypeDeclaration(kind))
                {
                    yield return member;
                }
            }
        }
    }
}