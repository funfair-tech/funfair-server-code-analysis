using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
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
public sealed class ConstructorGenericParameterTypeDiagnosticsAnalyser : DiagnosticAnalyzer
{
    private static readonly IReadOnlyList<TypeCheckSpec> Specifications =
    [
        Build(
            ruleId: Rules.LoggerParametersOnBaseClassesShouldNotUseGenericLoggerCategory,
            title: "ILogger parameters on base classes should not be ILogger<{0}> but ILogger",
            message: "ILogger parameters on base classes should not be ILogger<{0}> but ILogger",
            allowedSourceClass: "Microsoft.Extensions.Logging.ILogger",
            prohibitedClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
            isProtected: true,
            matchTypeOnGenericParameters: false
        ),
        Build(
            ruleId: Rules.LoggerParametersOnLeafClassesShouldUseGenericLoggerCategory,
            title: "ILogger parameters on leaf classes should not be ILogger but ILogger<{0}>",
            message: "ILogger parameters on leaf classes should not be ILogger but ILogger<{0}>",
            allowedSourceClass: "Microsoft.Extensions.Logging.ILogger<TCategoryName>",
            prohibitedClass: "Microsoft.Extensions.Logging.ILogger",
            isProtected: false,
            matchTypeOnGenericParameters: true
        ),
    ];

    private static readonly DiagnosticDescriptor MissMatchTypes = RuleHelpers.CreateRule(
        code: Rules.GenericTypeMissMatch,
        category: Categories.Naming,
        title: "Should be using '{0}' rather than '{1}' with {2}",
        message: "Should be using '{0}' rather than '{1}' with {2}"
    );

    private static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnosticsCache =
        SupportedDiagnosisList.Build(Specifications.Select(s => s.Rule)).Add(MissMatchTypes);

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
            action: checker.MustHaveSaneGenericUsages,
            SyntaxKind.ConstructorDeclaration
        );
    }

    private static TypeCheckSpec Build(
        string ruleId,
        string title,
        string message,
        string allowedSourceClass,
        string prohibitedClass,
        bool isProtected,
        bool matchTypeOnGenericParameters
    )
    {
        return new(
            ruleId: ruleId,
            title: title,
            message: message,
            allowedSourceClass: allowedSourceClass,
            prohibitedClass: prohibitedClass,
            isProtected: isProtected,
            matchTypeOnGenericParameters: matchTypeOnGenericParameters
        );
    }

    private sealed class Checker
    {
        private readonly Dictionary<(bool isProtected, string fullTypeName), TypeCheckSpec?> _specCache = [];
        private readonly Dictionary<ISymbol, string> _classNameCache = new(SymbolEqualityComparer.Default);
        private readonly Dictionary<ISymbol, bool> _protectionCache = new(SymbolEqualityComparer.Default);

        public void MustHaveSaneGenericUsages(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            if (syntaxNodeAnalysisContext.Node is not ConstructorDeclarationSyntax constructorDeclarationSyntax)
            {
                return;
            }

            if (constructorDeclarationSyntax.Parent is not ClassDeclarationSyntax parentSymbolForClassForConstructor)
            {
                return;
            }

            ISymbol classForConstructor = GetDeclaredSymbol(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parentSymbolForClassForConstructor: parentSymbolForClassForConstructor
            );

            string className = this.GetOrCacheClassName(classForConstructor);

            bool isProtected = this.GetOrCacheProtection(
                parentSymbolForClassForConstructor: parentSymbolForClassForConstructor,
                classForConstructor: classForConstructor,
                constructorDeclarationSyntax: constructorDeclarationSyntax
            );

            constructorDeclarationSyntax.ParameterList.Parameters
                .ForEach(parameterSyntax =>
                    this.CheckParameter(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        parameterSyntax: parameterSyntax,
                        isProtected: isProtected,
                        className: className
                    ));
        }

        private string GetOrCacheClassName(ISymbol classForConstructor)
        {
            if (this._classNameCache.TryGetValue(key: classForConstructor, out string? cachedName))
            {
                return cachedName;
            }

            string className = classForConstructor.ToDisplayString();
            this._classNameCache[classForConstructor] = className;
            return className;
        }

        private bool GetOrCacheProtection(
            ClassDeclarationSyntax parentSymbolForClassForConstructor,
            ISymbol classForConstructor,
            ConstructorDeclarationSyntax constructorDeclarationSyntax
        )
        {
            if (this._protectionCache.TryGetValue(key: classForConstructor, out bool cachedProtection))
            {
                return cachedProtection;
            }

            bool isProtected = IsNeeded(
                parentSymbolForClassForConstructor: parentSymbolForClassForConstructor,
                classForConstructor: classForConstructor,
                constructorDeclarationSyntax: constructorDeclarationSyntax
            );

            this._protectionCache[classForConstructor] = isProtected;
            return isProtected;
        }

        [SuppressMessage(
            category: "Nullable.Extended.Analyzer",
            checkId: "NX0001: Suppression of NullForgiving operator is not required",
            Justification = "Required here"
        )]
        private static INamedTypeSymbol GetDeclaredSymbol(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ClassDeclarationSyntax parentSymbolForClassForConstructor
        )
        {
            return syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(
                declarationSyntax: parentSymbolForClassForConstructor,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            )!;
        }

        private static bool IsNeeded(
            ClassDeclarationSyntax parentSymbolForClassForConstructor,
            ISymbol classForConstructor,
            ConstructorDeclarationSyntax constructorDeclarationSyntax
        )
        {
            bool isProtected = constructorDeclarationSyntax.Modifiers
                .Any(x => x.IsKind(SyntaxKind.ProtectedKeyword));

            if (isProtected)
            {
                return true;
            }

            bool classIsNested = classForConstructor.ContainingType is not null;

            if (classIsNested)
            {
                return true;
            }

            bool classIsPublic = parentSymbolForClassForConstructor.Modifiers
                .Any(x => x.IsKind(SyntaxKind.PublicKeyword));

            return !classIsPublic;
        }

        private void CheckParameter(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ParameterSyntax parameterSyntax,
            bool isProtected,
            string className
        )
        {
            string? fullTypeName = ParameterHelpers.GetFullTypeName(
                syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                parameterSyntax: parameterSyntax,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

            if (fullTypeName is null)
            {
                return;
            }

            TypeCheckSpec? checkRule = this.GetTypeSpec(isProtected: isProtected, fullTypeName: fullTypeName);

            if (checkRule is null)
            {
                return;
            }

            TypeCheckSpec rule = checkRule.Value;

            if (rule.IsAllowedSourceClass(fullTypeName))
            {
                if (rule.MatchTypeOnGenericParameters)
                {
                    CheckGenericParameterTypeMatch(
                        syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                        parameterSyntax: parameterSyntax,
                        className: className,
                        fullTypeName: fullTypeName
                    );
                }

                return;
            }

            if (rule.IsProhibitedClass(fullTypeName))
            {
                parameterSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: rule.Rule,
                    className
                );
            }
        }

        private TypeCheckSpec? GetTypeSpec(bool isProtected, string fullTypeName)
        {
            (bool isProtected, string fullTypeName) key = (isProtected, fullTypeName);

            if (this._specCache.TryGetValue(key: key, out TypeCheckSpec? cachedSpec))
            {
                return cachedSpec;
            }

            TypeCheckSpec? spec = Specifications.FirstOrDefault(ns => ns.IsAllowedSourceClass( isProtected: isProtected, fullTypeName: fullTypeName));

            this._specCache[key] = spec;
            return spec;
        }

        private static void CheckGenericParameterTypeMatch(
            in SyntaxNodeAnalysisContext syntaxNodeAnalysisContext,
            ParameterSyntax parameterSyntax,
            string className,
            string fullTypeName
        )
        {
            IParameterSymbol? ds = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(
                declarationSyntax: parameterSyntax,
                cancellationToken: syntaxNodeAnalysisContext.CancellationToken
            );

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

            string displayName = tm[0].ToDisplayString();

            if (!StringComparer.Ordinal.Equals(x: displayName, y: className))
            {
                parameterSyntax.ReportDiagnostics(
                    syntaxNodeAnalysisContext: syntaxNodeAnalysisContext,
                    rule: MissMatchTypes,
                    className,
                    displayName,
                    fullTypeName
                );
            }
        }
    }

    [DebuggerDisplay(
        "{Rule.Id} {Rule.Title} Allowed {AllowedSourceClass} Prohibited {ProhibitedClass} Match on generics {MatchTypeOnGenericParameters}"
    )]
    private readonly record struct TypeCheckSpec
    {
        private static readonly StringComparer TypeComparer = StringComparer.Ordinal;

        public TypeCheckSpec(
            string ruleId,
            string title,
            string message,
            string allowedSourceClass,
            string prohibitedClass,
            bool isProtected,
            bool matchTypeOnGenericParameters
        )
        {
            this.AllowedSourceClass = allowedSourceClass;
            this.ProhibitedClass = prohibitedClass;
            this.IsProtected = isProtected;
            this.MatchTypeOnGenericParameters = matchTypeOnGenericParameters;

            this.Rule = RuleHelpers.CreateRule(
                code: ruleId,
                category: Categories.Naming,
                title: title,
                message: message
            );
        }

        public string AllowedSourceClass { get; }

        public string ProhibitedClass { get; }

        public bool IsProtected { get; }

        public bool MatchTypeOnGenericParameters { get; }

        public DiagnosticDescriptor Rule { get; }

        public bool IsAllowedSourceClass(string fullTypeName)
        {
            return TypeComparer.Equals(x: this.AllowedSourceClass, y: fullTypeName);
        }

        public bool IsProhibitedClass(string fullTypeName)
        {
            return TypeComparer.Equals(x: this.ProhibitedClass, y: fullTypeName);
        }

        public bool IsAllowedSourceClass( bool isProtected, string fullTypeName)
        {
            return this.IsProtected == isProtected
                   && (this.IsAllowedSourceClass(fullTypeName) || this.IsProhibitedClass(fullTypeName));
        }
    }
}