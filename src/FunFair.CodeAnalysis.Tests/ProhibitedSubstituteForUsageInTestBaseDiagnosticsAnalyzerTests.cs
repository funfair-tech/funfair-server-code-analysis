using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ProhibitedSubstituteForUsageInTestBaseDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ProhibitedSubstituteForUsageInTestBaseDiagnosticsAnalyzer>
{
    [Fact]
    public Task SubstituteForIsBannedInClassDerivedFromTestBaseAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace ConsoleApplication1
{
    public interface IMyService
    {
    }

    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            IMyService service = Substitute.For<IMyService>();
        }
    }
}";
        DiagnosticResult expected = Result(
            id: "FFS0052",
            message: "Use GetSubstitute<T>() instead of Substitute.For<T>() in classes derived from TestBase; if registering the substitute with an IServiceCollection, use serviceCollection.AddMockedService<T>() instead of AddSingleton",
            severity: DiagnosticSeverity.Error,
            line: 17,
            column: 34
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ],
            expected: expected
        );
    }

    [Fact]
    public Task SubstituteForGenericLoggerIsBannedInClassDerivedFromTestBaseAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            ILogger<TypeName> logger = Substitute.For<ILogger<TypeName>>();
        }
    }
}";
        DiagnosticResult expected = Result(
            id: "FFS0053",
            message: "Use this.GetTypedLogger<T>() instead of Substitute.For<ILogger<T>>() in classes derived from TestBase",
            severity: DiagnosticSeverity.Error,
            line: 14,
            column: 40
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
                WellKnownMetadataReferences.GenericLogger,
                WellKnownMetadataReferences.Logger,
            ],
            expected: expected
        );
    }

    [Fact]
    public Task SubstituteForIsAllowedInClassNotDerivedFromTestBaseAsync()
    {
        const string test =
            @"
using NSubstitute;

namespace ConsoleApplication1
{
    public interface IMyService
    {
    }

    public sealed class TypeName
    {
        public void DoIt()
        {
            IMyService service = Substitute.For<IMyService>();
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ]
        );
    }

    [Fact]
    public Task AnalyzerDoesNothingWhenNSubstituteIsNotReferencedAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon]
        );
    }

    [Fact]
    public Task MethodCallWithoutMemberAccessIsIgnoredAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            LocalCall();
        }

        private static void LocalCall()
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ]
        );
    }

    [Fact]
    public Task DelegateFieldInvocationIsIgnoredAsync()
    {
        const string test =
            @"
using System;
using FunFair.Test.Common;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        private readonly Action _action = () => { };

        [Fact]
        public void DoIt()
        {
            this._action();
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ]
        );
    }

    [Fact]
    public Task MethodNotNamedForIsIgnoredAsync()
    {
        const string test =
            @"
using System;
using FunFair.Test.Common;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            Console.WriteLine(""Hello"");
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ]
        );
    }

    [Fact]
    public Task MethodNamedForOnUnrelatedClassIsIgnoredAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using Xunit;

namespace ConsoleApplication1
{
    public static class LocalFactory
    {
        public static T For<T>()
            where T : class, new()
        {
            return new T();
        }
    }

    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            LocalFactory.For<TypeName>();
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
            ]
        );
    }

    [Fact]
    public Task SubstituteForArrayTypeArgumentIsNotConsideredLoggerAsync()
    {
        const string test =
            @"
using FunFair.Test.Common;
using NSubstitute;
using Xunit;

namespace ConsoleApplication1
{
    public sealed class TypeName : TestBase
    {
        [Fact]
        public void DoIt()
        {
            int[] items = Substitute.For<int[]>();
        }
    }
}";
        DiagnosticResult expected = Result(
            id: "FFS0052",
            message: "Use GetSubstitute<T>() instead of Substitute.For<T>() in classes derived from TestBase; if registering the substitute with an IServiceCollection, use serviceCollection.AddMockedService<T>() instead of AddSingleton",
            severity: DiagnosticSeverity.Error,
            line: 13,
            column: 27
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            [
                WellKnownMetadataReferences.Xunit,
                WellKnownMetadataReferences.FunFairTestCommon,
                WellKnownMetadataReferences.Substitute,
                WellKnownMetadataReferences.GenericLogger,
                WellKnownMetadataReferences.Logger,
            ],
            expected: expected
        );
    }
}
