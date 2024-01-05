using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedClassesInTestAssembliesDiagnosticsAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ProhibitedClassesInTestAssembliesDiagnosticsAnalyzer();
    }

    [Fact]
    public Task AssertTrueForConsoleUsageInTestAsync()
    {
        const string test = @"
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
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0041",
                                        Message = "Use ITestOutputHelper rather than System.Console in test projects",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 18)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Xunit
                                                ],
                                                expected);
    }

    [Fact]
    public Task ConsoleIsAllowedInNonTestsAsync()
    {
        const string test = @"
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