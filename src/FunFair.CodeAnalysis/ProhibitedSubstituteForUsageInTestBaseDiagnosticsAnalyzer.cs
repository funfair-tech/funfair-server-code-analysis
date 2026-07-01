using System;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ProhibitedSubstituteForUsageInTestBaseDiagnosticsAnalyzer : DiagnosticAnalyzer
{
    private const string SUBSTITUTE_CLASS = "NSubstitute.Substitute";
    private const string SUBSTITUTE_METHOD = "For";
    private const string GENERIC_LOGGER_CLASS = "Microsoft.Extensions.Logging.ILogger`1";

    private static readonly DiagnosticDescriptor RuleSubstituteFor = RuleHelpers.CreateRule(
        code: Rules.RuleDontUseSubstituteForInTestBase,
        category: Categories.IllegalMethodCalls,
        title: "Avoid direct use of Substitute.For<T>() in classes derived from TestBase",
        message: "Use GetSubstitute<T>() instead of Substitute.For<T>() in classes derived from TestBase; if registering the substitute with an IServiceCollection, use serviceCollection.AddMockedService<T>() instead of AddSingleton"
    );

    private static readonly DiagnosticDescriptor RuleSubstituteForLogger = RuleHelpers.CreateRule(
        code: Rules.RuleDontUseSubstituteForILoggerInTestBase,
        category: Categories.IllegalMethodCalls,
        title: "Avoid direct use of Substitute.For<ILogger<T>>() in classes derived from TestBase",
        message: "Use this.GetTypedLogger<T>() instead of Substitute.For<ILogger<T>>() in classes derived from TestBase"
    );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [RuleSubstituteFor, RuleSubstituteForLogger];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(PerformCheck);
    }

    private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
    {
        INamedTypeSymbol? substituteType = compilationStartContext.Compilation.GetTypeByMetadataName(SUBSTITUTE_CLASS);

        if (substituteType is null)
        {
            return;
        }

        INamedTypeSymbol? genericLoggerType = compilationStartContext.Compilation.GetTypeByMetadataName(
            GENERIC_LOGGER_CLASS
        );

        Checker checker = new(substituteType: substituteType, genericLoggerType: genericLoggerType);

        compilationStartContext.RegisterSyntaxNodeAction(
            action: checker.LookForBannedSubstituteForUsage,
            SyntaxKind.InvocationExpression
        );
    }

    private sealed class Checker
    {
        private readonly INamedTypeSymbol _substituteType;
        private readonly INamedTypeSymbol? _genericLoggerType;

        public Checker(INamedTypeSymbol substituteType, INamedTypeSymbol? genericLoggerType)
        {
            this._substituteType = substituteType;
            this._genericLoggerType = genericLoggerType;
        }

        [SuppressMessage(
            category: "Roslynator.Analyzers",
            checkId: "RCS1231:Make parameter ref read only",
            Justification = "Needed here"
        )]
        public void LookForBannedSubstituteForUsage(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            InvocationExpressionSyntax invocation = (InvocationExpressionSyntax)syntaxNodeAnalysisContext.Node;

            IMethodSymbol? methodSymbol = this.GetSubstituteForMethodSymbol(
                invocation: invocation,
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext
            );

            if (methodSymbol is null)
            {
                return;
            }

            if (!TestDetection.IsDerivedFromTestBase(syntaxNodeAnalysisContext))
            {
                return;
            }

            DiagnosticDescriptor rule = this.IsGenericLoggerTypeArgument(methodSymbol)
                ? RuleSubstituteForLogger
                : RuleSubstituteFor;

            invocation.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: rule);
        }

        private IMethodSymbol? GetSubstituteForMethodSymbol(
            InvocationExpressionSyntax invocation,
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext
        )
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccessExpressionSyntax)
            {
                return null;
            }

            if (
                syntaxNodeAnalysisContext
                    .SemanticModel.GetSymbolInfo(
                        node: memberAccessExpressionSyntax,
                        cancellationToken: syntaxNodeAnalysisContext.CancellationToken
                    )
                    .Symbol
                is not IMethodSymbol methodSymbol
            )
            {
                return null;
            }

            if (!StringComparer.Ordinal.Equals(x: methodSymbol.Name, y: SUBSTITUTE_METHOD))
            {
                return null;
            }

            if (!SymbolEqualityComparer.Default.Equals(methodSymbol.ContainingType, this._substituteType))
            {
                return null;
            }

            return methodSymbol.TypeArguments.Length == 1 ? methodSymbol : null;
        }

        private bool IsGenericLoggerTypeArgument(IMethodSymbol methodSymbol)
        {
            if (this._genericLoggerType is null)
            {
                return false;
            }

            if (methodSymbol.TypeArguments[0] is not INamedTypeSymbol typeArgument)
            {
                return false;
            }

            return SymbolEqualityComparer.Default.Equals(typeArgument.ConstructedFrom, this._genericLoggerType);
        }
    }
}
