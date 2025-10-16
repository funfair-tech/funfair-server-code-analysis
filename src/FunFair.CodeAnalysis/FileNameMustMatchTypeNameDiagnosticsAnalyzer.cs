using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class FileNameMustMatchTypeNameDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleTypeShouldBeInAFileWithSameName,
        category: Categories.Files,
        title: "Should be in a file of the same name as the type",
        message: "Should be in a file of the same name as the type"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

    private static readonly StringComparer FileNameComparer = StringComparer.OrdinalIgnoreCase;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        // Create a checker instance per compilation for caching
        Checker checker = new();

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.CheckTypeNames,
            SyntaxKind.CompilationUnit
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<MemberDeclarationSyntax, string> _typeNameCache = [];

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void CheckTypeNames(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not CompilationUnitSyntax compilationUnitSyntax)
            {
                return;
            }

            string fileName = GetDocumentFileName(compilationUnitSyntax);
            IReadOnlyList<MemberDeclarationSyntax> members = GetNonNestedTypeDeclarations(compilationUnitSyntax);

            members.Select(member => (Member: member, TypeName: this.GetTypeName(member)))
                   .Where(FileNameIsDifferent)
                   .ForEach(item => item.Member.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule));

            bool FileNameIsDifferent((MemberDeclarationSyntax Member, string TypeName) item)
            {
                return !string.IsNullOrWhiteSpace(item.TypeName) && !FileNameComparer.Equals(x: fileName, y: item.TypeName);
            }
        }

        private static string GetDocumentFileName(CompilationUnitSyntax compilationUnitSyntax)
        {
            string filePath = compilationUnitSyntax.SyntaxTree.FilePath;

            string fileName = Path.GetFileNameWithoutExtension(filePath);
            return GetSplitFileName(fileName);
        }

        private static string GetSplitFileName(string fileName)
        {
            int split = fileName.IndexOf('.');

            return split == -1
                ? fileName
                : fileName.Substring(startIndex: 0, length: split);
        }

        private string GetTypeName(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            // Check cache first
            if (this._typeNameCache.TryGetValue(key: memberDeclarationSyntax, out string? cachedName))
            {
                return cachedName;
            }

            // Compute and cache result
            string typeName = memberDeclarationSyntax switch
            {
                ClassDeclarationSyntax classDecl => classDecl.Identifier.ToString(),
                RecordDeclarationSyntax recordDecl => recordDecl.Identifier.ToString(),
                StructDeclarationSyntax structDecl => structDecl.Identifier.ToString(),
                InterfaceDeclarationSyntax interfaceDecl => interfaceDecl.Identifier.ToString(),
                EnumDeclarationSyntax enumDecl => enumDecl.Identifier.ToString(),
                _ => string.Empty,
            };

            this._typeNameCache[memberDeclarationSyntax] = typeName;
            return typeName;
        }

        private static IReadOnlyList<MemberDeclarationSyntax> GetNonNestedTypeDeclarations(
            CompilationUnitSyntax compilationUnit
        )
        {
            return [.. GetNonNestedTypeDeclarations(compilationUnit.Members)];
        }

        private static IEnumerable<MemberDeclarationSyntax> GetNonNestedTypeDeclarations(
            in SyntaxList<MemberDeclarationSyntax> members
        )
        {
            return members.SelectMany(GetMemberDeclarations)
                          .RemoveNulls();
        }

        private static IEnumerable<MemberDeclarationSyntax?> GetMemberDeclarations(MemberDeclarationSyntax member)
        {
            SyntaxKind kind = member.Kind();

            if (kind == SyntaxKind.NamespaceDeclaration)
            {
                NamespaceDeclarationSyntax namespaceDeclaration = (NamespaceDeclarationSyntax)member;
                return GetNonNestedTypeDeclarations(namespaceDeclaration.Members);
            }

            if (SyntaxFacts.IsTypeDeclaration(kind))
            {
                return [member];
            }

            return [];
        }
    }
}