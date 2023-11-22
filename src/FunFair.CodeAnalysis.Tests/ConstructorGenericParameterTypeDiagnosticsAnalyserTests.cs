using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ConstructorGenericParameterTypeDiagnosticsAnalyserTests : DiagnosticVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ConstructorGenericParameterTypeDiagnosticsAnalyser();
    }

    [Fact]
    public Task SealedClassUsesOwnClassNameInConstructorParameterIsNotAnErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public sealed class Test {

    public Test(ILogger<Test> logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }

    [Fact]
    public Task SealedClassUsesUnnamedNameInConstructorParameterIsAnErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public sealed class Test {

    public Test(ILogger logger)
    {
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0024",
                                        Message = "ILogger parameters on leaf classes should not be ILogger but ILogger<Test>",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 17)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Logger
                                                ],
                                                expected);
    }

    [Fact]
    public Task BaseClassUsesOwnClassNameInConstructorParameterIsAnErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public abstract class Test {

    protected Test(ILogger<Test> logger)
    {
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0023",
                                        Message = "ILogger parameters on base classes should not be ILogger<Test> but ILogger",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 6, column: 20)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Logger
                                                ],
                                                expected);
    }

    [Fact]
    public Task BaseClassUsesUnnamedNameInConstructorParameterIsNotAnErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public abstract class Test {

    protected Test(ILogger logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }

    [Fact]
    public Task SealedClassNotUsingItsOwnNameIsAnErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public sealed class Banana
{
}

public sealed class Test {

    public Test(ILogger<Banana> logger)
    {
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0025",
                                        Message = "Should be using 'Test' rather than 'Banana' with Microsoft.Extensions.Logging.ILogger<TCategoryName>",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[]
                                                    {
                                                        new DiagnosticResultLocation(path: "Test0.cs", line: 10, column: 17)
                                                    }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test,
                                                [
                                                    WellKnownMetadataReferences.Logger
                                                ],
                                                expected);
    }

    [Fact]
    public Task SealedInternalClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

internal sealed class Test {

    public Test(ILogger logger)
    {
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }

    [Fact]
    public Task NestedSealedInternalClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public sealed class Onion {

    internal sealed class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }

    [Fact]
    public Task NestedSealedPrivateClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public sealed class Onion {

    private sealed class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }

    [Fact]
    public Task NestedSealedProtectedClassNotUsingItsOwnNameIsAnNotErrorAsync()
    {
        const string test = @"
using Microsoft.Extensions.Logging;

public class Onion {

    protected class Test {

        public Test(ILogger logger)
        {
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test,
        [
            WellKnownMetadataReferences.Logger
        ]);
    }
}