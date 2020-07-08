using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodsDiagnosticsAnalyzer();
        }

        [Fact]
        public Task DateTimeNowIsBannedInConstructorsAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class TypeName
        {
            private readonly DateTime _when;

            public TypeName()
            {
                _when = DateTime.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeNowIsBannedInConversionOperatorsAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class TypeName
        {
            private readonly DateTime _when = DateTime.MinValue;

             public static explicit operator DateTime(TypeName left)
             {
                 return DateTime.Now;
             }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 25)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeNowIsBannedInMethodsAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTime.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeNowIsBannedInOperatorsAsync()
        {
            const string test = @"
    using System;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        public class TypeName
        {
            private readonly DateTime _when = DateTime.MinValue;

            public static bool operator ==(TypeName left, TypeName right)
            {
                Debug.Write(DateTime.Now);
                return left == right;
            }

            public static bool operator !=(TypeName left, TypeName right)
            {
                return left == right;
            }

            public override int GetHashCode()
            {
                return 1;
            }
            
            public override bool Equals(object obj)
            {
                return false;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 13, column: 29)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeNowIsBannedInPropertiesAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public DateTime Test
            {
                get
                {
                    return DateTime.Now;
                }
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeOffsetNowIsBannedAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTimeOffset.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0004",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeOffsetUtcNowIsBannedAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTimeOffset.UtcNow;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0005",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeTodayIsBannedAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTime.Today;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0003",
                                            Message = @"Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task DateTimeUtcNowIsBannedAsync()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                var when = DateTime.UtcNow;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0002",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 28)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task ExecuteArbitrarySqlAsyncIsBannedAsync()
        {
            const string test = @"
    using System.Threading.Tasks;
    namespace FunFair.Common.Data
    {
         public interface ISqlServerDatabase
         {
                Task ExecuteArbitrarySqlAsync(string sql);
         }
    }

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test(FunFair.Common.Data.ISqlServerDatabase sqlServerDatabase, string sql)
            {
                sqlServerDatabase.ExecuteArbitrarySqlAsync(sql);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0006",
                                            Message = @"Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 18, column: 17)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task NoErrorsReportedAsync()
        {
            const string test = @"";

            return this.VerifyCSharpDiagnosticAsync(test);
        }

        [Fact]
        public Task QueryArbitrarySqlAsyncGenericIsBannedAsync()
        {
            const string test = @"

    using System.Threading.Tasks;

    namespace FunFair.Common.Data
    {
         public interface ISqlServerDatabase
         {
                Task QueryArbitrarySqlAsync<T>(string sql);
         }
    }

    namespace ConsoleApplication1
    {
        class Test
        {
            public int Id { get; set; }
        }

        class TypeName
        {
            void Test(FunFair.Common.Data.ISqlServerDatabase sqlServerDatabase, string sql)
            {
                sqlServerDatabase.QueryArbitrarySqlAsync<Test>(sql);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0007",
                                            Message = @"Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 24, column: 17)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task QueryArbitrarySqlAsyncIsBannedAsync()
        {
            const string test = @"
    using System.Threading.Tasks;

    namespace FunFair.Common.Data
    {
         public interface ISqlServerDatabase
         {
                Task QueryArbitrarySqlAsync(string sql);
         }
    }

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test(FunFair.Common.Data.ISqlServerDatabase sqlServerDatabase, string sql)
            {
                sqlServerDatabase.QueryArbitrarySqlAsync(sql);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0007",
                                            Message = @"Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 18, column: 17)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }
    }
}