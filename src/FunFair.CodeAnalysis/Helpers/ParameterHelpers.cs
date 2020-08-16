namespace FunFair.CodeAnalysis.Helpers
{
    internal static class ParameterHelpers
    {
        public static string? GetFullTypeName(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext, ParameterSyntax parameterSyntax)
        {
            IParameterSymbol? ds = syntaxNodeAnalysisContext.SemanticModel.GetDeclaredSymbol(parameterSyntax);

            if (ds != null)
            {
                ITypeSymbol typeSymbol = GetTypeSymbol(ds);

                return typeSymbol.ToDisplayString();
            }

            return null;
        }

        private static ITypeSymbol GetTypeSymbol(IParameterSymbol ds)
        {
            ITypeSymbol dsType = ds.Type;

            if (dsType is INamedTypeSymbol nts && nts.IsGenericType)
            {
                dsType = dsType.OriginalDefinition;
            }

            return dsType;
        }
    }
}