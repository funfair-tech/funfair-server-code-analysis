﻿using System.Threading.Tasks;
using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ReThrowingExceptionShouldSpecifyInnerExceptionDiagnosticsAnalyzer();
        }

        [Fact]
        public Task ReThrowingExceptionShouldNotTriggerErrorAsync()
        {
            const string test = @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception exception)
        {
            Console.WriteLine(exception.Message);
            throw;
        }
    }
}";

            return this.VerifyCSharpDiagnosticAsync(source: test);
        }

        [Fact]
        public Task ThrowingNewExceptionPassingInnerExceptionShouldNotBeAnErrorAsync()
        {
            const string test = @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception exception)
        {
            Console.WriteLine(exception.Message);
            throw new NotImplementedException(""Not Implemented yet"", exception);
        }
    }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0017",
                                            Message = "Provide 'exception' as a inner exception when throw from the catch clauses",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 14, column: 19)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }

        [Fact]
        public Task ThrowingNewExceptionWithoutPassingInnerExceptionShouldBeAnErrorAsync()
        {
            const string test = @"
using System;

public sealed class Test {

    public void DoIt()
    {
        try
        {
        }
        catch(Exception failingException)
        {
            Console.WriteLine(exception.Message);
            throw new NotImplementedException();
        }
    }
}";

            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0017",
                                            Message = "Provide 'failingException' as a inner exception when throw from the catch clauses",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 14, column: 19)}
                                        };

            return this.VerifyCSharpDiagnosticAsync(source: test, expected);
        }
    }
}