using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedMethodsDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ProhibitedMethodsDiagnosticsAnalyzer>
{
    [Fact]
    public Task DateTimeNowIsBannedInConstructorsAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0001",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            severity: DiagnosticSeverity.Error,
            line: 12,
            column: 25
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeNowIsBannedInConversionOperatorsAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0001",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            severity: DiagnosticSeverity.Error,
            line: 12,
            column: 25
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeNowIsBannedInMethodsAsync()
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
                var when = DateTime.Now;
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0001",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeNowIsBannedInOperatorsAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0001",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            severity: DiagnosticSeverity.Error,
            line: 13,
            column: 29
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeNowIsBannedInPropertiesAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0001",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.Now",
            severity: DiagnosticSeverity.Error,
            line: 12,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeOffsetNowIsBannedAsync()
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
                var when = DateTimeOffset.Now;
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0004",
            message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.Now",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeOffsetUtcNowIsBannedAsync()
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
                var when = DateTimeOffset.UtcNow;
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0005",
            message: "Call IDateTimeSource.UtcNow() rather than DateTimeOffset.UtcNow",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeTodayIsBannedAsync()
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
                var when = DateTime.Today;
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0003",
            message: "Call IDateTimeSource.UtcNow().Date rather than DateTime.Today",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task DateTimeUtcNowIsBannedAsync()
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
                var when = DateTime.UtcNow;
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0002",
            message: "Call IDateTimeSource.UtcNow() rather than DateTime.UtcNow",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task ExecuteArbitrarySqlAsyncIsBannedAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0006",
            message: "Only use ISqlServerDatabase.ExecuteArbitrarySqlAsync in integration tests",
            severity: DiagnosticSeverity.Error,
            line: 18,
            column: 17
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task NoErrorsReportedAsync()
    {
        const string test = "";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task QueryArbitrarySqlAsyncGenericIsBannedAsync()
    {
        const string test =
            @"

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
        DiagnosticResult expected = Result(
            id: "FFS0007",
            message: "Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
            severity: DiagnosticSeverity.Error,
            line: 24,
            column: 17
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task QueryArbitrarySqlAsyncIsBannedAsync()
    {
        const string test =
            @"
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
        DiagnosticResult expected = Result(
            id: "FFS0007",
            message: "Only use ISqlServerDatabase.QueryArbitrarySqlAsync in integration tests",
            severity: DiagnosticSeverity.Error,
            line: 18,
            column: 17
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task RemoteIpAddressIsBannedAsync()
    {
        const string test =
            @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                System.Net.IPAddress connectionRemoteIpAddress = new Microsoft.AspNetCore.Http.DefaultHttpContext().Connection.RemoteIpAddress;
            }
        }
    }";

        DiagnosticResult expected = Result(
            id: "FFS0026",
            message: "Use RemoteIpAddressRetriever",
            severity: DiagnosticSeverity.Error,
            line: 8,
            column: 66
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.IpAddress,
                WellKnownMetadataReferences.ConnectionInfo,
                WellKnownMetadataReferences.HttpContext,
            ],
            expected: expected
        );
    }

    [Fact]
    public Task RemoteIpAddressIsBannedWithUsingsAsync()
    {
        const string test =
            @"
    using System.Net;
    using Microsoft.AspNetCore.Http;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Test()
            {
                IPAddress connectionRemoteIpAddress = new DefaultHttpContext().Connection.RemoteIpAddress;
            }
        }
    }";

        DiagnosticResult expected = Result(
            id: "FFS0026",
            message: "Use RemoteIpAddressRetriever",
            severity: DiagnosticSeverity.Error,
            line: 11,
            column: 55
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.IpAddress,
                WellKnownMetadataReferences.ConnectionInfo,
                WellKnownMetadataReferences.HttpContext,
            ],
            expected: expected
        );
    }

    [Fact]
    public Task GuidParseIsBannedInMethodsAsync()
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
                var when = Guid.Parse(""66D3F243-10D5-4A17-9676-6F258BB56A46"");
            }
        }
    }";
        DiagnosticResult expected = Result(
            id: "FFS0037",
            message: "Use new Guid() with constant guids or Guid.TryParse everywhere else",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 28
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }
}
