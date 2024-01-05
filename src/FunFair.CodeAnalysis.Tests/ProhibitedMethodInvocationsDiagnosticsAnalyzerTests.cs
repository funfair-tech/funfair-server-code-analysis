using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<ProhibitedMethodInvocationsDiagnosticsAnalyzer>
{
    [Fact]
    public Task AssertFalseWithMessageIsAllowedAsync()
    {
        const string test = @"
     using Xunit;
     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Assert.False(false, ""Somevalue"");
             }
         }
     }";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Assert);
    }

    [Fact]
    public Task AssertFalseWithoutMessageIsBannedAsync()
    {
        const string test = @"
    using Xunit;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                Assert.False(false);
            }
        }
    }";
        DiagnosticResult expected = Result(id: "FFS0010", message: "Only use Assert.False with message parameter", severity: DiagnosticSeverity.Error, line: 9, column: 17);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Assert, expected: expected);
    }

    [Fact]
    public Task AssertTrueWithMessageIsAllowedAsync()
    {
        const string test = @"
    using Xunit;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                Assert.True(true, ""Somevalue"");
            }
        }
    }";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Assert);
    }

    [Fact]
    public Task AssertTrueWithoutMessageIsBannedAsync()
    {
        const string test = @"
     using Xunit;
     namespace ConsoleApplication1
     {
         class TypeName
         {
             void Test()
             {
                 Assert.True(1==1);
             }
         }
     }";
        DiagnosticResult expected = Result(id: "FFS0009", message: "Only use Assert.True with message parameter", severity: DiagnosticSeverity.Error, line: 9, column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Assert
                                                ],
                                                expected: expected);
    }

    [Fact]
    public Task ShouldRaiseErrorForAddOrUpdateAsync()
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
                 dictionary.AddOrUpdate(key: 1, addValueFactory: (x) => x + 1, updateValueFactory: (x,y)=> x+y+1);
             }
         }
     }";
        DiagnosticResult expected = Result(id: "FFS0032",
                                           message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 10,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task ShouldRaiseErrorForAddOrUpdateWithOutAddValueFactoryAsync()
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
                 dictionary.AddOrUpdate(key: 1, addValue: 1, updateValueFactory: (x, y) => x + y);
             }
         }
     }";
        DiagnosticResult expected = Result(id: "FFS0032",
                                           message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 10,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.NonBlockingConcurrentDictionary
                                                ],
                                                expected: expected);
    }

    [Fact]
    public Task ShouldRaiseForAddOrUpdateAsync()
    {
        const string test = @"
     using NonBlocking;
     namespace ConsoleApplication1
     {
         class TypeName
         {
             int Update(int x, int y){
                return x+y+1;
            }
            int Add(int x){
                return x+1;
            }
             void Test()
             {
                 ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int,int>();
                 dictionary.AddOrUpdate(key: 1, addValueFactory: this.Add, updateValueFactory: this.Update);
             }
         }
     }";

        DiagnosticResult expected = Result(id: "FFS0032",
                                           message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 16,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.NonBlockingConcurrentDictionary
                                                ],
                                                expected: expected);
    }

    [Fact]
    public Task ShouldRaiseForAddOrUpdateWithAddValueFactoryAsync()
    {
        const string test = @"
     using NonBlocking;
     namespace ConsoleApplication1
     {
         class TypeName
         {
             int Update(int x, int y){
                return x+y+1;
            }
             void Test()
             {
                 ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int,int>();
                 dictionary.AddOrUpdate(key: 1, addValue: 1, updateValueFactory: this.Update);
             }
         }
     }";

        DiagnosticResult expected = Result(id: "FFS0032",
                                           message: "Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 13,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task ShouldRaiseErrorForGetOrAddAsync()
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
                 dictionary.GetOrAdd(key: 1, valueFactory: i => i);
             }
         }
     }";
        DiagnosticResult expected = Result(id: "FFS0033",
                                           message: "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 10,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task ShouldRaiseForGetOrAddWithValueFactoryAsync()
    {
        const string test = @"
     using NonBlocking;
     namespace ConsoleApplication1
     {
         class TypeName
         {
             int ValueFactory(int x){
                return x+1;
            }
             void Test()
             {
                 ConcurrentDictionary<int,int> dictionary = new ConcurrentDictionary<int,int>();
                 dictionary.GetOrAdd(key: 1, valueFactory: this.ValueFactory);
             }
         }
     }";
        DiagnosticResult expected = Result(id: "FFS0033",
                                           message: "Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
                                           severity: DiagnosticSeverity.Error,
                                           line: 13,
                                           column: 18);

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary, expected: expected);
    }

    [Fact]
    public Task ShouldAllowGetOrAddAsync()
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
                 dictionary.GetOrAdd(key: 1, value: 1);
             }
         }
     }";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.NonBlockingConcurrentDictionary);
    }
}