using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

internal static class CompilationExtensions
{
    private static readonly IReadOnlyList<string> TestAssemblies =
    [
        "Microsoft.NET.Test.Sdk"
    ];

    private static readonly IReadOnlyList<string> UnitTestAssemblies =
    [
        "Microsoft.NET.Test.Sdk",
        "xunit",
        "xunit.core"
    ];

    private static bool Matches(IReadOnlyList<string> assemblyNames, AssemblyIdentity assembly)
    {
        return assemblyNames.Contains(assembly.Name, StringComparer.OrdinalIgnoreCase);
    }

    public static bool IsTestAssembly(this Compilation compilation)
    {
        try
        {
            return compilation.ReferencedAssemblyNames.Any(assemblyName => Matches(TestAssemblies, assemblyName));
        }
        catch (Exception exception)
        {
            // note this shouldn't occur; Line here for debugging
            Debug.WriteLine(exception.Message);

            return false;
        }
    }

    public static bool IsUnitTestAssembly(this Compilation compilation)
    {
        try
        {
            return compilation.ReferencedAssemblyNames.Any(assemblyName => Matches(UnitTestAssemblies, assemblyName));
        }
        catch (Exception exception)
        {
            // note this shouldn't occur; Line here for debugging
            Debug.WriteLine(exception.Message);

            return false;
        }
    }
}