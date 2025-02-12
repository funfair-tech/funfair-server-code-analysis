using System.Diagnostics;

namespace FunFair.CodeAnalysis.Helpers;

[DebuggerDisplay(value: "{ClassName}.{MethodName}")]
internal readonly record struct Mapping
{
    public Mapping(string methodName, string className)
    {
        this.MethodName = methodName;
        this.ClassName = className;
    }

    private string MethodName { get; }

    private string ClassName { get; }

    public string QualifiedName => string.Concat(str0: this.ClassName, str1: ".", str2: this.MethodName);
}
