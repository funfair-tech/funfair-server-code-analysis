using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class OneTypePerDocumentAnalysisDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private const string ENUM_TYPE_PREFIX = "enum:";
    private const string INTERFACE_TYPE_PREFIX = "interface:";
    private const string STRUCT_TYPE_PREFIX = "struct:";
    private const string RECORD_TYPE_PREFIX = "record:";
    private const string STATIC_TYPE_PREFIX = "static:";
    private const string CLASS_TYPE_PREFIX = "class:";

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(code: Rules.RuleOnlyOneTypeDefinedPerFile,
                                                                               category: Categories.Files,
                                                                               title: "Should be only one type per file",
                                                                               message: "Should be only one type per file");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosisList.Build(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        compilationStartContext.RegisterSyntaxNodeAction(action: MustContainOneType, SyntaxKind.CompilationUnit);
    }

    private static void MustContainOneType(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is not CompilationUnitSyntax compilationUnitSyntax)
        {
            return;
        }

        IReadOnlyList<MemberDeclarationSyntax> members = GetNonNestedTypeDeclarations(compilationUnitSyntax)
            .ToArray();

        IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> grouped = GroupedMembers(members);

        switch (grouped.Count)
        {
            case <= 1:
            case 2 when IsSpecialCaseWhereNonGenericHelperClassExists(grouped): return;
            default:
                ReportAllMembersAsErrors(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, members: members);

                break;
        }
    }

    private static IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> GroupedMembers(IReadOnlyList<MemberDeclarationSyntax> members)
    {
        return members.GroupBy(keySelector: GetTypeName, comparer: StringComparer.Ordinal)
                      .Where(x => !string.IsNullOrWhiteSpace(x.Key))
                      .ToArray();
    }

    private static bool IsSpecialCaseWhereNonGenericHelperClassExists(IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> grouped)
    {
        IGrouping<string, MemberDeclarationSyntax>? staticGrouping = grouped.FirstOrDefault(g => IsStatic(g.Key));

        if (staticGrouping is null)
        {
            return false;
        }

        IGrouping<string, MemberDeclarationSyntax> otherGrouping = grouped.First(g => !IsStatic(g.Key));

        return IsClass(otherGrouping.Key) || IsStruct(otherGrouping.Key);
    }

    private static bool IsStatic(string key)
    {
        return key.StartsWith(value: STATIC_TYPE_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    private static bool IsClass(string key)
    {
        return key.StartsWith(value: CLASS_TYPE_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    private static bool IsStruct(string key)
    {
        return key.StartsWith(value: STRUCT_TYPE_PREFIX, comparisonType: StringComparison.Ordinal);
    }

    private static void ReportAllMembersAsErrors(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, IReadOnlyList<MemberDeclarationSyntax> members)
    {
        foreach (MemberDeclarationSyntax member in members)
        {
            member.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule);
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
        return string.Concat(str0: ENUM_TYPE_PREFIX, enumDeclarationSyntax.Identifier.ToString());
    }

    private static string NormaliseInterface(InterfaceDeclarationSyntax interfaceDeclarationSyntax)
    {
        return string.Concat(str0: INTERFACE_TYPE_PREFIX, interfaceDeclarationSyntax.Identifier.ToString());
    }

    private static string NormaliseRecord(StructDeclarationSyntax structDeclarationSyntax)
    {
        return string.Concat(str0: STRUCT_TYPE_PREFIX, structDeclarationSyntax.Identifier.ToString());
    }

    private static string NormaliseStruct(RecordDeclarationSyntax recordDeclarationSyntax)
    {
        return string.Concat(str0: RECORD_TYPE_PREFIX, recordDeclarationSyntax.Identifier.ToString());
    }

    private static string NormaliseClass(ClassDeclarationSyntax classDeclarationSyntax)
    {
        if (classDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
        {
            return string.Concat(str0: STATIC_TYPE_PREFIX, classDeclarationSyntax.Identifier.ToString());
        }

        return string.Concat(str0: CLASS_TYPE_PREFIX, classDeclarationSyntax.Identifier.ToString());
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