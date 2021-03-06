﻿using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis
{
    /// <summary>
    ///     Looks for issues with parameter types in constructor
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class ConstructorGenericParameterTypeDiagnosticsAnalyser : DiagnosticAnalyzer
    {
        private const string CATEGORY = "Naming";

        private static readonly TypeCheckSpec[] Specifications =
        {
            new(ruleId: Rules.LoggerParametersOnBaseClassesShouldNotUseGenericLoggerCategory,
                title: "ILogger parameters on base classes should not be ILogger<{0}> but ILogger",
                message: "ILogger parameters on base classes should not be ILogger<{0}> but ILogger",
                allowedSourceClass: "Microsoft.Extensions.Logging.ILogger",
                prohibitedClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                isProtected: true,
                matchTypeOnGenericParameters: false),
            new(ruleId: Rules.LoggerParametersOnLeafClassesShouldUseGenericLoggerCategory,
                title: "ILogger parameters on leaf classes should not be ILogger but ILogger<{0}>",
                message: "ILogger parameters on leaf classes should not be ILogger but ILogger<{0}>",
                allowedSourceClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                prohibitedClass: "Microsoft.Extensions.Logging.ILogger",
                isProtected: false,
                matchTypeOnGenericParameters: true)
        };

        private static readonly DiagnosticDescriptor MissMatchTypes = RuleHelpers.CreateRule(code: Rules.GenericTypeMissMatch,
                                                                                             category: CATEGORY,
                                                                                             title: "Should be using '{0}' rather than '{1}' with {2}",
                                                                                             message: "Should be using '{0}' rather than '{1}' with {2}");

        /// <inheritdoc />
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            Specifications.Select(selector: r => r.Rule)
                          .Concat(new[] {MissMatchTypes})
                          .ToImmutableArray();

        /// <inheritdoc />
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();

            context.RegisterCompilationStartAction(PerformCheck);
        }

        private static void PerformCheck(CompilationStartAnalysisContext compilationStartContext)
        {
            compilationStartContext.RegisterSyntaxNodeAction(action: MustHaveSaneGenericUsages, SyntaxKind.ConstructorDeclaration);
        }

        private static void MustHaveSaneGenericUsages(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is ConstructorDeclarationSyntax constructorDeclarationSyntax)
            {
                if (constructorDeclarationSyntax.Parent! is ClassDeclarationSyntax parentSymbolForClassForConstructor)
                {
                    ISymbol classForConstructor = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(parentSymbolForClassForConstructor)!;
                    string className = classForConstructor.ToDisplayString();

                    bool classIsPublic = parentSymbolForClassForConstructor.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword));

                    bool classIsNested = classForConstructor.ContainingType != null;

                    bool isProtected = constructorDeclarationSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword));

                    bool needed = isProtected || !classIsPublic || classIsNested;

                    foreach (ParameterSyntax parameterSyntax in constructorDeclarationSyntax.ParameterList.Parameters)
                    {
                        CheckParameter(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax, isProtected: needed, className: className);
                    }
                }
            }
        }

        private static void CheckParameter(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ParameterSyntax parameterSyntax, bool isProtected, string className)
        {
            string? fullTypeName = ParameterHelpers.GetFullTypeName(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax);

            if (fullTypeName == null)
            {
                return;
            }

            TypeCheckSpec? rule =
                Specifications.FirstOrDefault(ns => ns.IsProtected == isProtected && (ns.AllowedSourceClass == fullTypeName || ns.ProhibitedClass == fullTypeName));

            if (rule == null)
            {
                return;
            }

            if (rule.AllowedSourceClass == fullTypeName)
            {
                if (rule.MatchTypeOnGenericParameters)
                {
                    CheckGenericParameterTypeMatch(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                   parameterSyntax: parameterSyntax,
                                                   className: className,
                                                   fullTypeName: fullTypeName);
                }

                return;
            }

            if (rule.ProhibitedClass == fullTypeName)
            {
                syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: rule.Rule, parameterSyntax.GetLocation(), className));
            }
        }

        private static void CheckGenericParameterTypeMatch(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
                                                           ParameterSyntax parameterSyntax,
                                                           string className,
                                                           string fullTypeName)
        {
            IParameterSymbol? ds = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(parameterSyntax);

            if (ds != null)
            {
                ITypeSymbol dsType = ds.Type;

                if (dsType is INamedTypeSymbol nts && nts.IsGenericType)
                {
                    ImmutableArray<ITypeSymbol> tm = nts.TypeArguments;

                    if (tm.Length == 1)
                    {
                        string displayName = tm[0]
                            .ToDisplayString();

                        if (displayName != className)
                        {
                            syntaxNodeAnalysisContext.ReportDiagnostic(Diagnostic.Create(descriptor: MissMatchTypes,
                                                                                         parameterSyntax.GetLocation(),
                                                                                         className,
                                                                                         displayName,
                                                                                         fullTypeName));
                        }
                    }
                }
            }
        }

        private sealed class TypeCheckSpec
        {
            public TypeCheckSpec(string ruleId,
                                 string title,
                                 string message,
                                 string allowedSourceClass,
                                 string prohibitedClass,
                                 bool isProtected,
                                 bool matchTypeOnGenericParameters)
            {
                this.AllowedSourceClass = allowedSourceClass;
                this.ProhibitedClass = prohibitedClass;
                this.IsProtected = isProtected;
                this.MatchTypeOnGenericParameters = matchTypeOnGenericParameters;

                this.Rule = RuleHelpers.CreateRule(code: ruleId, category: CATEGORY, title: title, message: message);
            }

            public string AllowedSourceClass { get; }

            public string ProhibitedClass { get; }

            public bool IsProtected { get; }

            public bool MatchTypeOnGenericParameters { get; }

            public DiagnosticDescriptor Rule { get; }
        }
    }
}