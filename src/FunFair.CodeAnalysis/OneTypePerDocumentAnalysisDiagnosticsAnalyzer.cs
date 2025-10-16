using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
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

    private static readonly DiagnosticDescriptor Rule = RuleHelpers.CreateRule(
        code: Rules.RuleOnlyOneTypeDefinedPerFile,
        category: Categories.Files,
        title: "Should be only one type per file",
        message: "Should be only one type per file"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Rule);

    private static readonly StringComparer TypeNameComparer = StringComparer.Ordinal;

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => SupportedDiagnosticsCache;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        Checker checker = new();

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.MustContainOneType,
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
        public void MustContainOneType(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not CompilationUnitSyntax compilationUnitSyntax)
            {
                return;
            }

            IReadOnlyList<MemberDeclarationSyntax> members = GetNonNestedTypeDeclarations(compilationUnitSyntax);
            IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> grouped = this.GroupedMembers(members);

            switch (grouped.Count)
            {
                case <= 1:
                case 2 when IsSpecialCaseWhereNonGenericHelperClassExists(grouped):
                    return;
                default:
                    ReportAllMembersAsErrors(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, members: members);
                    break;
            }
        }

        private IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> GroupedMembers(
            IReadOnlyList<MemberDeclarationSyntax> members
        )
        {
            return
            [
                .. members
                    .GroupBy(keySelector: this.GetTypeName, comparer: TypeNameComparer)
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key)),
            ];
        }

        private static bool IsSpecialCaseWhereNonGenericHelperClassExists(
            IReadOnlyList<IGrouping<string, MemberDeclarationSyntax>> grouped
        )
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

        private static void ReportAllMembersAsErrors(
            SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            IReadOnlyList<MemberDeclarationSyntax> members
        )
        {
            members.ForEach(member =>
                member.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: Rule)
            );
        }

        private string GetTypeName(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            if (this._typeNameCache.TryGetValue(key: memberDeclarationSyntax, out string? cachedName))
            {
                return cachedName;
            }

            string typeName = FindTypeName(memberDeclarationSyntax);

            this._typeNameCache[memberDeclarationSyntax] = typeName;
            return typeName;
        }

        private static string FindTypeName(MemberDeclarationSyntax memberDeclarationSyntax)
        {
            return memberDeclarationSyntax switch
            {
                ClassDeclarationSyntax classDeclarationSyntax => NormaliseClass(classDeclarationSyntax),
                RecordDeclarationSyntax recordDeclarationSyntax => NormaliseRecord(recordDeclarationSyntax),
                StructDeclarationSyntax structDeclarationSyntax => NormaliseStruct(structDeclarationSyntax),
                InterfaceDeclarationSyntax interfaceDeclarationSyntax => NormaliseInterface(interfaceDeclarationSyntax),
                EnumDeclarationSyntax enumDeclarationSyntax => NormaliseEnum(enumDeclarationSyntax),
                _ => string.Empty,
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

        private static string NormaliseStruct(StructDeclarationSyntax structDeclarationSyntax)
        {
            return string.Concat(str0: STRUCT_TYPE_PREFIX, structDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseRecord(RecordDeclarationSyntax recordDeclarationSyntax)
        {
            return string.Concat(str0: RECORD_TYPE_PREFIX, recordDeclarationSyntax.Identifier.ToString());
        }

        private static string NormaliseClass(ClassDeclarationSyntax classDeclarationSyntax)
        {
            bool isStatic = classDeclarationSyntax.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword));

            string prefix = isStatic ? STATIC_TYPE_PREFIX : CLASS_TYPE_PREFIX;
            return string.Concat(str0: prefix, classDeclarationSyntax.Identifier.ToString());
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
            return members.SelectMany(GetMemberDeclarations).RemoveNulls();
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
