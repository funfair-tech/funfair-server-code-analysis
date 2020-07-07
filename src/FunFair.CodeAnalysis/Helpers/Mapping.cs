using System;

namespace FunFair.CodeAnalysis.Helpers
{
    /// <summary>
    ///     Mapping class
    /// </summary>
    public sealed class Mapping : IEquatable<Mapping>
    {
        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="methodName">Method name</param>
        /// <param name="className">Class name</param>
        public Mapping(string methodName, string className)
        {
            this.MethodName = methodName;
            this.ClassName = className;
        }

        /// <summary>
        ///     Method name
        /// </summary>
        public string MethodName { get; }

        /// <summary>
        ///     Class name
        /// </summary>
        public string ClassName { get; }

        /// <summary>
        ///     Full qualified name of method
        /// </summary>
        public string QualifiedName => string.Concat(str0: this.ClassName, str1: ".", str2: this.MethodName);

        /// <inheritdoc />
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

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return ReferenceEquals(this, objB: obj) || obj is Mapping other && this.Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return (this.MethodName.GetHashCode() * 397) ^ this.ClassName.GetHashCode();
        }

        /// <summary>
        ///     Operator ==
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Result</returns>
        public static bool operator ==(Mapping? left, Mapping? right)
        {
            return Equals(objA: left, objB: right);
        }

        /// <summary>
        ///     Operator !=
        /// </summary>
        /// <param name="left">Left</param>
        /// <param name="right">Right</param>
        /// <returns>Result</returns>
        public static bool operator !=(Mapping? left, Mapping? right)
        {
            return !Equals(objA: left, objB: right);
        }
    }
}