using System;

namespace FunFair.CodeAnalysis.Tests.Exceptions
{
    /// <summary>
    ///     Raised when the verified code is invalid.
    /// </summary>
    public sealed class UnitTestSourceException : Exception
    {
        /// <summary>
        ///     Constructor.
        /// </summary>

        // ReSharper disable once UnusedMember.Global
        public UnitTestSourceException()
            : this(message: "House not ready")
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">The message to return.</param>
        public UnitTestSourceException(string message)
            : base(message)
        {
        }

        /// <summary>
        ///     Constructor.
        /// </summary>
        /// <param name="message">The message to return.</param>
        /// <param name="innerException">The inner exception.</param>

        // ReSharper disable once UnusedMember.Global
        public UnitTestSourceException(string message, Exception innerException)
            : base(message: message, innerException: innerException)
        {
        }
    }
}