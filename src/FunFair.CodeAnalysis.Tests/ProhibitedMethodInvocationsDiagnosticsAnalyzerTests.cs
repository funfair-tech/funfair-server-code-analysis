using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodInvocationsDiagnosticsAnalyzer();
        }

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

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert});
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0010",
                                            Message = @"Only use Assert.False with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 17)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert}, expected);
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

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert});
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0009",
                                            Message = @"Only use Assert.True with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 9, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Assert}, expected);
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0032",
                                            Message = @"Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected: expected);
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0032",
                                            Message = @"Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected: expected);
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

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0032",
                                            Message = @"Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 16, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected: expected);
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

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0032",
                                            Message = @"Don't use any of the built in AddOrUpdate methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 13, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected);
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0033",
                                            Message = @"Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected);
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
            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0033",
                                            Message = @"Don't use any of the built in GetOrAdd methods, instead FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd can be used",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 13, column: 18)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary}, expected);
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

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.NonBlockingConcurrentDictionary});
        }
    }
}