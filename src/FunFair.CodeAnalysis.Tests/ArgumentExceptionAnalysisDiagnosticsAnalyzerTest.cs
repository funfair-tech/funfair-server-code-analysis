using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ArgumentExceptionAnalysisDiagnosticsAnalyzerTest : CodeFixVerifier
{
    protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
    {
        return new ArgumentExceptionAnalysisDiagnosticsAnalyzer();
    }

    [Fact]
    public Task ArgumentExceptionWhenParameterLessConstructorUsedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentException();
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentExceptionWhenParameterNameNotPassedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentException(""Hello World"");
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentExceptionWhenParameterNamePassedByNameOfIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentException(""Hello World"", nameof(value));

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ArgumentExceptionWhenParameterNamePassedByStringIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentException(""Hello World"", ""value"");

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ArgumentNullExceptionWhenParameterLessConstructorUsedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentNullException();
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentNullExceptionWhenParameterNameNotPassedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentNullException(""Hello World"", new Exception());
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentNullExceptionWhenParameterNamePassedByNameOfIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentNullException(""Hello World"", nameof(value));

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ArgumentNullExceptionWhenParameterNamePassedByStringIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentNullException(""Hello World"", ""value"");

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ArgumentOutOfRangeExceptionWhenParameterLessConstructorUsedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentOutOfRangeException();
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentOutOfRangeExceptionWhenParameterNameNotPassedCausesErrorAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentOutOfRangeException(""Hello World"", new Exception());
    }
}";

        DiagnosticResult expected = new()
                                    {
                                        Id = "FFS0016",
                                        Message = "Argument Exceptions should pass parameter name",
                                        Severity = DiagnosticSeverity.Error,
                                        Locations = new[] { new DiagnosticResultLocation(path: "Test0.cs", line: 8, column: 17) }
                                    };

        return this.VerifyCSharpDiagnosticAsync(source: test, expected);
    }

    [Fact]
    public Task ArgumentOutOfRangeExceptionWhenParameterNamePassedByNameOfIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentOutOfRangeException(""Hello World"", nameof(value));

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ArgumentOutOfRangeExceptionWhenParameterNamePassedByStringIsValidAsync()
    {
        const string test = @"
using System;

public sealed class Test {

    public void DoIt(string value)
    {
        var x = new ArgumentOutOfRangeException(""Hello World"", ""value"");

    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task DoesNotTriggerOnNonExceptionsAsync()
    {
        const string test = @"
public sealed class Test {

    public void DoIt(string value)
    {
        var x = new string(nameof(value));
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }
}