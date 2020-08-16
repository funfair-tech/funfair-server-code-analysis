using FunFair.CodeAnalysis.Helpers;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with parameter names
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ParameterNameDiagnosticsAnalyzer : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Naming";

        private static readonly NameSanitationSpec[] NameSpecifications =
        {
            new NameSanitationSpec(ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
                                   title: @"ILogger parameters should be called 'logger'",
                                   message: "ILogger parameters should be called 'logger'",
                                   sourceClass: "Microsoft.Extensions.Logging.ILogger",
                                   whitelistedParameterName: "logger"),
            new NameSanitationSpec(ruleId: Rules.RuleLoggerParametersShouldBeCalledLogger,
                                   title: @"ILogger parameters should be called 'logger'",
                                   message: "ILogger parameters should be called 'logger'",
                                   sourceClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                                   whitelistedParameterName: "logger")
        };

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            NameSpecifications.Select(selector: r => r.Rule)
                              .ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(this.PerformCheck);
        }

        private void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: MustHaveASaneName, SyntaxKind.Parameter);
        }

        private static void MustHaveASaneName(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is ParameterSyntax parameterSyntax)
            {
                string? fullTypeName = ParameterHelpers.GetFullTypeName(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax);

                if (fullTypeName != null)
                {
                    NameSanitationSpec? rule = NameSpecifications.FirstOrDefault(ns => ns.SourceClass == fullTypeName);

                    if (rule != null)
                    {
                        if (!rule.WhitelistedParameterNames.Contains(parameterSyntax.Identifier.Text))
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule.Rule, parameterSyntax.GetLocation()));
                        }
                    }
                }
            }
        }

        private sealed class NameSanitationSpec
        {
            public NameSanitationSpec(string ruleId, string title, string message, string sourceClass, string whitelistedParameterName)
                : this(ruleId: ruleId, title: title, message: message, sourceClass: sourceClass, new[] {whitelistedParameterName})
            {
            }

            public NameSanitationSpec(string ruleId, string title, string message, string sourceClass, string[] whitelistedParameterNames)
            {
                this.SourceClass = sourceClass;
                this.WhitelistedParameterNames = whitelistedParameterNames;

                this.Rule = CreateRule(code: ruleId, title: title, message: message);
            }

            public string SourceClass { get; }

            public IReadOnlyList<string> WhitelistedParameterNames { get; }

            public DiagnosticDescriptor Rule { get; }

            private static DiagnosticDescriptor CreateRule(string code, string title, string message)
            {
                LiteralString translatableTitle = new LiteralString(title);
                LiteralString translatableMessage = new LiteralString(message);

                return new DiagnosticDescriptor(id: code,
                                                title: translatableTitle,
                                                messageFormat: translatableMessage,
                                                category: CATEGORY,
                                                defaultSeverity: DiagnosticSeverity.Error,
                                                isEnabledByDefault: true,
                                                description: translatableMessage);
            }
        }
    }
}