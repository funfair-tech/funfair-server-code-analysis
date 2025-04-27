using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ConstructorGenericParameterTypeDiagnosticsAnalyserTests
    : DiagnosticAnalyzerVerifier<ConstructorGenericParameterTypeDiagnosticsAnalyser>
{
    [Fact]
    public Task SealedClassUsesOwnClassNameInConstructorParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public sealed class Test {

    public Test(ILogger<Test> logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }

    [Fact]
    public Task SealedClassUsesUnnamedNameInConstructorParameterIsAnErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public sealed class Test {

    public Test(ILogger logger)
    {
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0024",
            message: "ILogger parameters on leaf classes should not be ILogger but ILogger<Test>",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 17
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Logger,
            expected: expected
        );
    }

    [Fact]
    public Task BaseClassUsesOwnClassNameInConstructorParameterIsAnErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public abstract class Test {

    protected Test(ILogger<Test> logger)
    {
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0023",
            message: "ILogger parameters on base classes should not be ILogger<Test> but ILogger",
            severity: DiagnosticSeverity.Error,
            line: 6,
            column: 20
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Logger,
            expected: expected
        );
    }

    [Fact]
    public Task BaseClassUsesUnnamedNameInConstructorParameterIsNotAnErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public abstract class Test {

    protected Test(ILogger logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }

    [Fact]
    public Task SealedClassNotUsingItsOwnNameIsAnErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public sealed class Banana
{
}

public sealed class Test {

    public Test(ILogger<Banana> logger)
    {
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0025",
            message: "Should be using 'Test' rather than 'Banana' with Microsoft.Extensions.Logging.ILogger<TCategoryName>",
            severity: DiagnosticSeverity.Error,
            line: 10,
            column: 17
        );

        return this.VerifyCSharpDiagnosticAsync(
            source: test,
            reference: WellKnownMetadataReferences.Logger,
            expected: expected
        );
    }

    [Fact]
    public Task SealedInternalClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

internal sealed class Test {

    public Test(ILogger logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }

    [Fact]
    public Task NestedSealedInternalClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public sealed class Onion {

    internal sealed class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }

    [Fact]
    public Task NestedSealedPrivateClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public sealed class Onion {

    private sealed class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }

    [Fact]
    public Task NestedSealedProtectedClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test =
            @"
using Microsoft.Extensions.Logging;

public class Onion {

    protected class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test, reference: WellKnownMetadataReferences.Logger);
    }
}
