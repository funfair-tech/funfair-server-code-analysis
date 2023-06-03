﻿using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using FunFair.CodeAnalysis.Extensions;
using FunFair.CodeAnalysis.Helpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis;

/// <summary>
///     Looks for issues with parameter types in constructor
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class ConstructorGenericParameterTypeDiagnosticsAnalyser : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<TypeCheckSpec> Specifications = new TypeCheckSpec[]
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
                                                                                         category: Categories.Naming,
                                                                                         title: "Should be using '{0}' rather than '{1}' with {2}",
                                                                                         message: "Should be using '{0}' rather than '{1}' with {2}");

    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        Specifications.Select(selector: r => r.Rule)
                      .Concat(new[]
                              {
                                  MissMatchTypes
                              })
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
        if (syntaxNodeAnalysisContext.Node is not ConstructorDeclarationSyntax constructorDeclarationSyntax)
        {
            return;
        }

        MustHaveSaneGenericUsages(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, constructorDeclarationSyntax: constructorDeclarationSyntax);
    }

    private static void MustHaveSaneGenericUsages(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ConstructorDeclarationSyntax constructorDeclarationSyntax)
    {
        if (constructorDeclarationSyntax.Parent is not ClassDeclarationSyntax parentSymbolForClassForConstructor)
        {
            return;
        }

        MustHaveSaneGenericUsages(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                  constructorDeclarationSyntax: constructorDeclarationSyntax,
                                  parentSymbolForClassForConstructor: parentSymbolForClassForConstructor);
    }

    private static void MustHaveSaneGenericUsages(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
                                                  ConstructorDeclarationSyntax constructorDeclarationSyntax,
                                                  ClassDeclarationSyntax parentSymbolForClassForConstructor)
    {
        ISymbol classForConstructor =
            syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(declarationSyntax: parentSymbolForClassForConstructor, cancellationToken: syntaxNodeAnalysisContext.CancellationToken)!;
        string className = classForConstructor.ToDisplayString();

        bool needed = IsNeeded(parentSymbolForClassForConstructor: parentSymbolForClassForConstructor,
                               classForConstructor: classForConstructor,
                               constructorDeclarationSyntax: constructorDeclarationSyntax);

        foreach (ParameterSyntax parameterSyntax in constructorDeclarationSyntax.ParameterList.Parameters)
        {
            CheckParameter(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax, isProtected: needed, className: className);
        }
    }

    private static bool IsNeeded(ClassDeclarationSyntax parentSymbolForClassForConstructor, ISymbol classForConstructor, ConstructorDeclarationSyntax constructorDeclarationSyntax)
    {
        bool isProtected = constructorDeclarationSyntax.Modifiers.Any(x => x.IsKind(SyntaxKind.ProtectedKeyword));

        if (isProtected)
        {
            return true;
        }

        bool classIsNested = classForConstructor.ContainingType is not null;

        if (classIsNested)
        {
            return true;
        }

        bool classIsPublic = parentSymbolForClassForConstructor.Modifiers.Any(x => x.IsKind(SyntaxKind.PublicKeyword));

        return !classIsPublic;
    }

    private static void CheckParameter(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ParameterSyntax parameterSyntax, bool isProtected, string className)
    {
        string? fullTypeName = ParameterHelpers.GetFullTypeName(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                                                                parameterSyntax: parameterSyntax,
                                                                cancellationToken: syntaxNodeAnalysisContext.CancellationToken);

        if (fullTypeName is null)
        {
            return;
        }

        TypeCheckSpec? rule = GetTypeSpec(isProtected: isProtected, fullTypeName: fullTypeName);

        if (rule is null)
        {
            return;
        }

        if (rule.AllowedSourceClass == fullTypeName)
        {
            if (rule.MatchTypeOnGenericParameters)
            {
                CheckGenericParameterTypeMatch(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, parameterSyntax: parameterSyntax, className: className, fullTypeName: fullTypeName);
            }

            return;
        }

        if (rule.ProhibitedClass == fullTypeName)
        {
            parameterSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: rule.Rule, className);
        }
    }

    private static TypeCheckSpec? GetTypeSpec(bool isProtected, string fullTypeName)
    {
        return Specifications.FirstOrDefault(ns => ns.IsProtected == isProtected && (ns.AllowedSourceClass == fullTypeName || ns.ProhibitedClass == fullTypeName));
    }

    private static void CheckGenericParameterTypeMatch(in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ParameterSyntax parameterSyntax, string className, string fullTypeName)
    {
        IParameterSymbol? ds = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(declarationSyntax: parameterSyntax, cancellationToken: syntaxNodeAnalysisContext.CancellationToken);

        ITypeSymbol? dsType = ds?.Type;

        if (dsType is not INamedTypeSymbol { IsGenericType: true } nts)
        {
            return;
        }

        ImmutableArray<ITypeSymbol> tm = nts.TypeArguments;

        if (tm.Length != 1)
        {
            return;
        }

        string displayName = tm[0]
            .ToDisplayString();

        if (displayName != className)
        {
            parameterSyntax.ReportDiagnostics(syntaxNodeAnalysisContext: syntaxNodeAnalysisContext, rule: MissMatchTypes, className, displayName, fullTypeName);
        }
    }

    private sealed class TypeCheckSpec
    {
        public TypeCheckSpec(string ruleId, string title, string message, string allowedSourceClass, string prohibitedClass, bool isProtected, bool matchTypeOnGenericParameters)
        {
            this.AllowedSourceClass = allowedSourceClass;
            this.ProhibitedClass = prohibitedClass;
            this.IsProtected = isProtected;
            this.MatchTypeOnGenericParameters = matchTypeOnGenericParameters;

            this.Rule = RuleHelpers.CreateRule(code: ruleId, category: Categories.Naming, title: title, message: message);
        }

        public string AllowedSourceClass { get; }

        public string ProhibitedClass { get; }

        public bool IsProtected { get; }

        public bool MatchTypeOnGenericParameters { get; }

        public DiagnosticDescriptor Rule { get; }
    }
}