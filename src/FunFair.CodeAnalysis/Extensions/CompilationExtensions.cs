using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

internal static class CompilationExtensions
{
    private static readonly HashSet<string> TestAssemblies = new([
                                                                     "Microsoft.NET.Test.Sdk"
                                                                 ],
                                                                 comparer: StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> UnitTestAssemblies = new([
                                                                         "Microsoft.NET.Test.Sdk",
                                                                         "xunit",
                                                                         "xunit.core"
                                                                     ],
                                                                     comparer: StringComparer.OrdinalIgnoreCase);

    public static bool IsTestAssembly(this Compilation compilation)
    {
        try
        {
            return compilation.ReferencedAssemblyNames.Any(assemblyName => TestAssemblies.Contains(assemblyName.Name));
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
            return compilation.ReferencedAssemblyNames.Any(assemblyName => UnitTestAssemblies.Contains(assemblyName.Name));
        }
        catch (Exception exception)
        {
            // note this shouldn't occur; Line here for debugging
            Debug.WriteLine(exception.Message);

            return false;
        }
    }
}