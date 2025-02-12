using System.Collections.Generic;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedClassesDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<ProhibitedClassesDiagnosticsAnalyzer>
{
    [Fact]
    public Task AssertFalseForNonBlockingConcurrentDictionaryAsync()
    {
        const string test =
            @"
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

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary);
    }

    [Fact]
    public Task AssertTrueForCreationOfNewInstanceWithMessageIsBannedAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0031",
            message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 61
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.ConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task AssertTrueForParameterInMethodDeclarationWithMessageIsBannedAsync()
    {
        const string test =
            @"
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

        DiagnosticResult expected = Result(
            id: "FFS0031",
            message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.ConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task AssertTrueForFieldDeclarationMessageIsBannedAsync()
    {
        const string test =
            @"
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

        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(
                id: "FFS0031",
                message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
                severity: DiagnosticSeverity.Error,
                line: 8,
                column: 52
            ),
            Result(
                id: "FFS0031",
                message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
                severity: DiagnosticSeverity.Error,
                line: 12,
                column: 35
            ),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, references: WellKnownMetadataReferences.ConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task AssertTrueForPropertyDeclarationMessageIsBannedAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0031",
            message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: 13
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, [WellKnownMetadataReferences.ConcurrentDictionary], expected: expected);
    }

    [Fact]
    public Task AssertTrueForParameterInConstructorDeclarationWithMessageIsBannedAsync()
    {
        const string test =
            @"
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

        DiagnosticResult expected = Result(
            id: "FFS0031",
            message: "Use NonBlocking.ConcurrentDictionary rather than System.Collections.Concurrent.ConcurrentDictionary",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: 14
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.ConcurrentDictionary, expected: expected);
    }
}
