using Microsoft.CodeAnalysis.Diagnostics;

namespace FunFair.CodeAnalysis.Tests.Verifiers;

public abstract class DiagnosticAnalyzerVerifier<TAnalyzer> : DiagnosticVerifier
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected sealed override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new TAnalyzer();
    }
}