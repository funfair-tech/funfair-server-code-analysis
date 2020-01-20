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
        public void DateTimeNowIsBannedInConstructors()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeNowIsBannedInConversionOperators()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeNowIsBannedInMethods()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeNowIsBannedInOperators()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        public class TypeName
        {
            private readonly DateTime _when = DateTime.MinValue;

            public static bool operator ==(TypeName left, TypeName right)
            {
                return left == right || left == DateTime.Now;
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0001",
                                            Message = @"Call IDateTimeSource.UtcNow() rather than DateTime.Now",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 12, column: 49)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeNowIsBannedInProperties()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeOffsetNowIsBanned()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeOffsetUtcNowIsBanned()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeTodayIsBanned()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void DateTimeUtcNowIsBanned()
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

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void QueryArbitrarySqlAsyncIsBanned()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test(FunFair.Common.Data.ISqlServerDatabase sqlServerDatabase, string sql)
            {
                sqlServerDatabase.QueryArbitrarySqlAsync(sql)
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0006",
                                            Message = @"Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 17)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void ExecuteArbitrarySqlAsyncIsBanned()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test(FunFair.Common.Data.ISqlServerDatabase sqlServerDatabase, string sql)
            {
                sqlServerDatabase.ExecuteArbitrarySqlAsync(sql)
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0006",
                                            Message = @"Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 17)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void NoErrorsReported()
        {
            const string test = @"";

            this.VerifyCSharpDiagnostic(test);
        }
    }
}