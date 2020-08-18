using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class TestClassAnalysisDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new TestClassAnalysisDiagnosticsAnalyzer();
        }

        [Fact]
        public Task ClassThatHasNothingToDoWithTestsIsNotAnErrorAsync()
        {
            const string test = @"
            public sealed class Test {

            public void DoIt()
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon});
        }

        [Fact]
        public Task FactClassThatDoesNotDeriveFromTestBaseIsAnErrorAsync()
        {
            const string test = @"
using Xunit;

            public sealed class Test {

            [Fact]
            public void DoIt()
            {
            }
}";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0013",
                                            Message = "Test classes should be derived from TestBase",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 13)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Xunit}, expected);
        }

        [Fact]
        public Task FactClassThatInheritsFromLoggingTestBaseIsNotAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common;
using Xunit;
using Xunit.Abstractions;

            public sealed class Test : LoggingTestBase {

            public Test(ITestOutputHelper output)
                : base(output)
            {
            }

            [Fact]
            public void DoIt()
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.Xunit,
                                                        WellKnownMetadataReferences.FunFairTestCommon,
                                                        WellKnownMetadataReferences.XunitAbstractions
                                                    });
        }

        [Fact]
        public Task FactClassThatInheritsFromTestBaseIsNotAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : TestBase {

            [Fact]
            public void DoIt()
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon});
        }

        [Fact]
        public Task TheoryClassThatDoesNotDeriveFromTestBaseIsAnErrorAsync()
        {
            const string test = @"
using Xunit;

            public sealed class Test {

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0013",
                                            Message = "Test classes should be derived from TestBase",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 13)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Xunit}, expected);
        }

        [Fact]
        public Task TheoryClassThatInheritsFromLoggingTestBaseIsNotAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common;
using Xunit;
using Xunit.Abstractions;

            public sealed class Test : LoggingTestBase {

            public Test(ITestOutputHelper output)
                : base(output)
            {
            }

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test,
                                                    new[]
                                                    {
                                                        WellKnownMetadataReferences.Xunit,
                                                        WellKnownMetadataReferences.FunFairTestCommon,
                                                        WellKnownMetadataReferences.XunitAbstractions
                                                    });
        }

        [Fact]
        public Task TheoryClassThatInheritsFromTestBaseIsNotAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common;
using Xunit;

            public sealed class Test : TestBase {

            [Theory]
            [InlineData(1)]
            public void DoIt(int i)
            {
            }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.Xunit, WellKnownMetadataReferences.FunFairTestCommon});
        }
    }
}