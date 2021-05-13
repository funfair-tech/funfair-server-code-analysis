using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ClassVisibilityDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ClassVisibilityDiagnosticsAnalyzer();
        }

        [Fact]
        public Task ClassThatHasNothingToDoWithTestsIsNotAnErrorAsync()
        {
            const string test = @"
public sealed class Test
{

    public void DoIt()
    {
    }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.FunFairTestCommon});
        }

        [Fact]
        public Task PublicMockBaseIsAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common.Mocks;

public sealed class Test : MockBase<string>
{
    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0029",
                                            Message = "MockBase<T> instances must be internal",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 4, column: 1)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.FunFairTestCommon}, expected);
        }

        [Fact]
        public Task InternalMockBaseNotAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common.Mocks;

internal sealed class Test : MockBase<string>
{

    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.FunFairTestCommon});
        }

        [Fact]
        public Task AbstractMockBaseIsAnErrorAsync()
        {
            const string test = @"
using FunFair.Test.Common.Mocks;

internal abstract class Test : MockBase<string>
{

    public Test()
       : base(string.Empty)
    {
    }

    public override string Next()
    {
        return string.Empty;
    }
}";

            DiagnosticResult expected = new()
                                        {
                                            Id = "FFS0030",
                                            Message = "MockBase<T> instances must be sealed",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 4, column: 1)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, new[] {WellKnownMetadataReferences.FunFairTestCommon}, expected);
        }
    }
}