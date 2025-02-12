using System.Collections.Generic;
using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class OneTypePerDocumentAnalysisDiagnosticsAnalyzerTests : DiagnosticAnalyzerVerifier<OneTypePerDocumentAnalysisDiagnosticsAnalyzer>
{
    [Fact]
    public Task OneClassDefinedInFileOkAsync()
    {
        const string test = "public sealed class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task ClassesOfSameNameDefinedInFileOkAsync()
    {
        const string test =
            @"public sealed class Test {}

public sealed class Test<T> {}

public sealed class Test<T1, T2> {}
";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task ClassesOfSameNameDefinedInFileOneGenericOneStaticExplicitlyOkAsync()
    {
        const string test =
            @"public sealed class Test<T> {}

public static class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task GenericStructAndStaticClassOfSameNameDefinedInFileOneGenericOneStaticExplicitlyOkAsync()
    {
        const string test =
            @"public readonly struct Test<T> {}

public static class Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task RecordsOfSameNameDefinedInFileOkAsync()
    {
        const string test =
            @"public sealed record Test {}

public sealed record Test<T> {}

public sealed record Test<T1, T2> {}
";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task StructsOfSameNameDefinedInFileOkAsync()
    {
        const string test =
            @"public readonly struct Test {}

public readonly struct Test<T> {}

public readonly struct Test<T1, T2> {}
";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task InterfacesOfSameNameDefinedInFileOkAsync()
    {
        const string test =
            @"public interface ITest {}

public interface ITest<T> {}

public interface ITest<T1, T2> {}
";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task ItemsOfSameNameButDifferentTypeDefinedInFileIsErrorAsync()
    {
        const string test =
            @"public sealed class Test {}

public readonly struct Test<T> {}

public sealed record Test<T1, T2> {}

public interface Test<T1, T2, T3> {}
";
        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 1, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 3, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 5, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 7, column: 1),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task ClassesOfDifferentNameDefinedInFileIsAnErrorAsync()
    {
        const string test =
            @"public sealed class Test {}

public sealed class Test1 {}
";
        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 1, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 3, column: 1),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task StructsOfDifferentNameDefinedInFileIsAnErrorAsync()
    {
        const string test =
            @"public readonly struct Test {}

public readonly struct Test1 {}
";

        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 1, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 3, column: 1),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task RecordsOfDifferentNameDefinedInFileIsAnErrorAsync()
    {
        const string test =
            @"public sealed record Test {}

public sealed record Test1 {}
";

        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 1, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 3, column: 1),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task InterfacesOfDifferentNameDefinedInFileIsAnErrorAsync()
    {
        const string test =
            @"public interface Test {}

public interface Test1 {}
";

        IReadOnlyList<DiagnosticResult> expected =
        [
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 1, column: 1),
            Result(id: "FFS0039", message: "Should be only one type per file", severity: DiagnosticSeverity.Error, line: 3, column: 1),
        ];

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task OneStructDefinedInFileOkAsync()
    {
        const string test = "public struct Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task OneRecordDefinedInFileOkAsync()
    {
        const string test = "public sealed record Test {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }

    [Fact]
    public Task OneInterfaceDefinedInFileOkAsync()
    {
        const string test = "public interface ITest {}";

        return this.VerifyCSharpDiagnosticAsync(test);
    }
}
