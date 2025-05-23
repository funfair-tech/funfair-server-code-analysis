using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedClassesInTestAssembliesDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ProhibitedClassesInTestAssembliesDiagnosticsAnalyzer>
{
    [Fact]
    public Task AssertTrueForConsoleUsageInTestAsync()
    {
        const string test =
            @"
     using System;
     using Xunit;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             [Fact]
             void Test()
             {
                 Console.WriteLine(""Hello World"");
             }
         }
     }";
        DiagnosticResult expected = Result(
            id: "FFS0041",
            message: "Use ITestOutputHelper rather than System.Console in test projects",
            severity: DiagnosticSeverity.Error,
            line: 12,
            column: 18
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, [WellKnownMetadataReferences.Xunit], expected: expected);
    }

    [Fact]
    public Task ConsoleIsAllowedInNonTestsAsync()
    {
        const string test =
            @"
     using System;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Console.WriteLine(""Hello World"");
             }
         }
     }";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }
}
