using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Extensions;

public static class CompilationExtensions
{
    private static readonly IReadOnlyList<string> TestAssemblies = new[]
                                                                   {
                                                                       @"Microsoft.NET.Test.Sdk"
                                                                   };

    public static bool IsTestAssembly(this Compilation compilation)
    {
        try
        {
            return compilation.ReferencedAssemblyNames.SelectMany(collectionSelector: _ => TestAssemblies, resultSelector: (assembly, testAssemblyName) => new { assembly, testAssemblyName })
                              .Where(t => StringComparer.InvariantCultureIgnoreCase.Equals(x: t.assembly.Name, y: t.testAssemblyName))
                              .Select(t => t.assembly)
                              .Any();
        }
        catch (Exception exception)
        {
            // note this shouldn't occur; Line here for debugging
            Debug.WriteLine(exception.Message);

            return false;
        }
    }
}