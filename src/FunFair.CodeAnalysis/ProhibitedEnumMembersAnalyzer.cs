using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedEnumMembersAnalyzer : DiagnosticAnalyzer
{
    private static readonly ProhibitedEmumsSpec[] BannedEnums = [
        Build(ruleId: Rules.RuleDontUseStringComparisonInvariantCulture,
              title: "Use System.StringComparison.Ordinal instead",
              message: "Use System.StringComparison.Ordinal instead",
              sourceEnum: "System.StringComparison", nameof(System.StringComparison.InvariantCulture)),
        Build(ruleId: Rules.RuleDontUseStringComparisonInvariantCultureIgnoreCase,
        title: "Use System.StringComparison.OrdinalIgnoreCase instead",
        message: "Use System.StringComparison.OrdinalIgnoreCase instead",
        sourceEnum: "System.StringComparison", nameof(System.StringComparison.InvariantCultureIgnoreCase))
    ];

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = [.. BannedEnums.Select(selector: r => r.Rule)];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        // Also check constants and assignments to variables
        compilationStartContext.RegisterSyntaxNodeAction(action: ParameterCannotBeProhibitedEnum, SyntaxKind.Parameter);
    }

    private static void ParameterCannotBeProhibitedEnum(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
    {
        if (syntaxNodeAnalysisContext.Node is ParameterSyntax)
        {
            Debug.WriteLine("Parameter");
        }
    }

    private static ProhibitedEmumsSpec Build(
        string ruleId,
        string title,
        string message,
        string sourceEnum,
        string bannedEnumValue
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            sourceEnum: sourceEnum,
            bannedEnumValue: bannedEnumValue
        );
    }

    private readonly record struct ProhibitedEmumsSpec
    {
        public ProhibitedEmumsSpec(string ruleId, string title, string message, string sourceEnum, string bannedEnumValue)
        {
            this.SourceEnum = sourceEnum;
            this.BannedEnumValue = bannedEnumValue;
            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.IllegalMethodCalls, title: title, message: message);
        }

        public string SourceEnum { get; }

        public string BannedEnumValue { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}