using System;
using System.Collections.Generic;
using System.Collections.Immutable;
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
    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleTypeShouldBeInAFileWithSameName,
                                                                               category: Categories.Files,
                                                                               title: "Should be in a file of the same name as the type",
                                                                               message: "Should be in a file of the same name as the type");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        new[]
        {
            Rule
        }.ToImmutableArray();

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: CheckTypeNames, SyntaxKind.CompilationUnit);
    }

    private static void CheckTypeNames(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return;
        }

        string fileName = GetDocumentFileName(compilationUnitSyntax);

        IReadOnlyList<MemberDeclarationSyntax> members = GetNonNestedTypeDeclarations(compilationUnitSyntax)
            .ToArray();

        foreach (MemberDeclarationSyntax member in members)
        {
            string name = GetTypeName(member);

            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            if (!StringComparer.InvariantCultureIgnoreCase.Equals(x: fileName, y: name))
            {
                member.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
            }
        }
    }

    private static string GetDocumentFileName(CompilationUnitSyntax compilationUnitSyntax)
    {
        string fileName = Path.GetFileNameWithoutExtension(compilationUnitSyntax.SyntaxTree.FilePath);
        int split = fileName.IndexOf('.');

        if (split != -1)
        {
            return fileName.Substring(startIndex: 0, length: split);
        }

        return fileName;
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
        return enumDeclarationSyntax.Identifier.ToString();
    }

    private static string NormaliseInterface(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
    {
        return interfaceDeclarationSyntax.Identifier.ToString();
    }

    private static string NormaliseRecord(StructDeclarationSyntax structDeclarationSyntax)
    {
        return structDeclarationSyntax.Identifier.ToString();
    }

    private static string NormaliseStruct(RecordDeclarationSyntax recordDeclarationSyntax)
    {
        return recordDeclarationSyntax.Identifier.ToString();
    }

    private static string NormaliseClass(ClassDeclarationSyntax classDeclarationSyntax)
    {
        return classDeclarationSyntax.Identifier.ToString();
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