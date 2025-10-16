using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Xunit;

namespace FunFair.CodeAnalysis.Tests;

public sealed class ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzerTests
    : DiagnosticAnalyzerVerifier<ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzer>
{
    [Fact]
    public Task ReThrowingExceptionShouldNotTriggerErrorAsync()
    {
        const string test =
            @"
    using System;

    namespace ConsoleApplication1
    {
        public class Test
        {
            public void DoIt()
            {
                    try
                    {
                    }
                    catch(Exception failingException)
                    {
                        Console.WriteLine(failingException.Message);
                        throw;
                    }
            }
         }
    }";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ThrowingNewExceptionPassingInnerExceptionShouldNotBeAnErrorAsync()
    {
        const string test =
            @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception failingException)
        {
            Console.WriteLine(failingException.Message);
            throw new NotImplementedException(""Not Implemented yet"", failingException);
        }
    }
}";

        return this.VerifyCSharpDiagnosticAsync(source: test);
    }

    [Fact]
    public Task ThrowingNewExceptionWithoutPassingInnerExceptionShouldBeAnErrorAsync()
    {
        const string test =
            @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception failingException)
        {
            Console.WriteLine(failingException.Message);
            throw new NotImplementedException(""Not Implemented yet"", new Exception(""Oops""));
        }
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0017",
            message: "Provide 'failingException' as an inner exception when thrown from catch clauses",
            severity: DiagnosticSeverity.Error,
            line: 14,
            column: 19
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }

    [Fact]
    public Task ThrowingNewExceptionWithoutPassingRandomExceptionShouldBeAnErrorAsync()
    {
        const string test =
            @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception failingException)
        {
            Console.WriteLine(failingException.Message);
            throw new NotImplementedException();
        }
    }
}";

        DiagnosticResult expected = Result(
            id: "FFS0017",
            message: "Provide 'failingException' as an inner exception when thrown from catch clauses",
            severity: DiagnosticSeverity.Error,
            line: 14,
            column: 19
        );

        return this.VerifyCSharpDiagnosticAsync(source: test, expected: expected);
    }
}
