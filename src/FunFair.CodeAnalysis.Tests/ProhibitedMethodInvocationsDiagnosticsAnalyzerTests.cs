using FunFair.CodeAnalysis.Tests.Helpers;
using FunFair.CodeAnalysis.Tests.Verifiers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace FunFair.CodeAnalysis.Tests
{
    public sealed class ProhibitedMethodInvocationsDiagnosticsAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new ProhibitedMethodInvocationsDiagnosticsAnalyzer();
        }

        [Fact]
        public void AssertTrueWithoutMessageIsBanned()
        {
            const string test = @"
    using System;

    namespace XUnit
    {
         public class Assert
         {
                public static void True(bool condition, string userMessage1)
                {
                }

                public static void True(bool condition)
                {
                }
         }
    }

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                XUnit.Assert.True(true);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0009",
                                            Message = @"Only use Assert.True with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 25, column: 17)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void AssertTrueWithMessageIsAllowed()
        {
            const string test = @"
    using System;

    namespace XUnit
    {
         public class Assert
         {
                public static void True(bool condition, string userMessage1)
                {
                }

                public static void True(bool condition)
                {
                }
         }
    }

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                XUnit.Assert.True(true, ""Somevalue"");
            }
        }
    }";
            this.VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void AssertFalseWithoutMessageIsBanned()
        {
            const string test = @"
    using System;

    namespace XUnit
    {
         public class Assert
         {
                public static void False(bool condition, string userMessage1)
                {
                }

                public static void False(bool condition)
                {
                }
         }
    }

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                XUnit.Assert.False(false);
            }
        }
    }";
            DiagnosticResult expected = new DiagnosticResult
                                        {
                                            Id = "FFS0010",
                                            Message = @"Only use Assert.False with message parameter",
                                            Severity = DiagnosticSeverity.Error,
                                            Locations = new[] {new DiagnosticResultLocation(path: "Test0.cs", line: 25, column: 17)}
                                        };

            this.VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void AssertFalseWithMessageIsAllowed()
        {
            const string test = @"
    using System;

    namespace XUnit
    {
         public class Assert
         {
                public static void False(bool condition, string userMessage1)
                {
                }

                public static void False(bool condition)
                {
                }
         }
    }

    namespace ConsoleApplication1
    {

        class TypeName
        {
            void Test()
            {
                XUnit.Assert.False(false, ""Somevalue"");
            }
        }
    }";
            this.VerifyCSharpDiagnostic(test);
        }
    }
}