using System;
using System.Diagnostics.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal sealed class Mapping : IEquatable<Mapping>
{
    public Mapping(string methodName, string className)
    {
        this.MethodName = methodName;
        this.ClassName = className;
    }

    private string MethodName { get; }

    private string ClassName { get; }

    public string QualifiedName => string.Concat(str0: this.ClassName, str1: ".", str2: this.MethodName);

    public bool Equals(Mapping? other)
    {
        if (ReferenceEquals(objA: null, objB: other))
        {
            return false;
        }

        if (ReferenceEquals(this, objB: other))
        {
            return true;
        }

        return this.MethodName == other.MethodName && this.ClassName == other.ClassName;
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, objB: obj) || obj is Mapping other && this.Equals(other);
    }

    [SuppressMessage(category: "Meziantou.Analyzer", checkId: "MA0021:Use String Comparer to compute hash codes", Justification = "Not in net stabdard 2.0")]
    public override int GetHashCode()
    {
        return (this.MethodName.GetHashCode() * 397) ^ this.ClassName.GetHashCode();
    }

    public static bool operator ==(Mapping? left, Mapping? right)
    {
        return Equals(objA: left, objB: right);
    }

    public static bool operator !=(Mapping? left, Mapping? right)
    {
        return !Equals(objA: left, objB: right);
    }
}