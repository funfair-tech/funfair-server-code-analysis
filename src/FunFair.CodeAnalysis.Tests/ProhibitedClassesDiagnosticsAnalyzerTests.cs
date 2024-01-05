using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedClassesDiagnosticsAnalyzerTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ProhibitedClassesDiagnosticsAnalyzer();
    }

    [Fact]
    public Task AssertFalseForNonBlockingConcurrentDictionaryAsync()
    {
        const string test = @"
     using NonBlocking;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int,int>();
             }
         }
     }";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.NonBlockingConcurrentDictionary
        ]);
    }

    [Fact]
    public Task AssertTrueForCreationOfNewInstanceWithMessageIsBannedAsync()
    {
        const string test = @"
     using System.Collections.Concurrent;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int,int>();
             }
         }
     }";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0031",
                                        Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 61)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.ConcurrentDictionary
                                                ],
                                                expected);
    }

    [Fact]
    public Task AssertTrueForParameterInMethodDeclarationWithMessageIsBannedAsync()
    {
        const string test = @"
     using System.Collections.Concurrent;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test(ConcurrentDictionary<int,int> dictionary)
             {

             }
         }
     }";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0031",
                                        Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 14)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.ConcurrentDictionary
                                                ],
                                                expected);
    }

    [Fact]
    public Task AssertTrueForFieldDeclarationMessageIsBannedAsync()
    {
        const string test = @"
     using System.Collections.Concurrent;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             private ConcurrentDictionary<int,int> dictionary;

             void Test()
             {
                this.dictionary = new ConcurrentDictionary<int,int>();
             }
         }
     }";
        DiagnosticResult[] expected =
        {
            new()
            {
                Id = "FFS0031",
                Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                            {
                                new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 52)
                            }
            },
            new()
            {
                Id = "FFS0031",
                Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                Severity = DiagnosticSeverity.Error,
                Locations = new[]
                            {
                                new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 35)
                            }
            }
        };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.ConcurrentDictionary
                                                ],
                                                expected: expected);
    }

    [Fact]
    public Task AssertTrueForPropertyDeclarationMessageIsBannedAsync()
    {
        const string test = @"
     using System.Collections.Concurrent;

     namespace ConsoleApplication1
     {
         class TypeName
         {
            public ConcurrentDictionary<int,int> Dictionary {get; private set;}

             void Test()
             {

             }
         }
     }";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0031",
                                        Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 13)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.ConcurrentDictionary
                                                ],
                                                expected);
    }

    [Fact]
    public Task AssertTrueForParameterInConstructorDeclarationWithMessageIsBannedAsync()
    {
        const string test = @"
     using System.Collections.Concurrent;

     namespace ConsoleApplication1
     {
         class TypeName
         {
             public TypeName(ConcurrentDictionary<int,int> dictionary)
             {

             }
         }
     }";
        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0031",
                                        Message = "Use NonBlocking.ConcurrentDictionary  rather than System.Collections.Concurrent.ConcurrentDictionary",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 14)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.ConcurrentDictionary
                                                ],
                                                expected);
    }
}