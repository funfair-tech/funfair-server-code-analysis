using System;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis
{
    internal sealed class LiteralString : LocalizableString
    {
        private readonly string _value;

        public LiteralString(string value)
        {
            this._value = value;
        }

        protected override string GetText(IFormatProvider? formatProvider)
        {
            return this._value;
        }

        protected override int GetHash()
        {
            return this._value.GetHashCode();
        }

        protected override bool AreEqual(object? other)
        {
            return other is LiteralString otherResourceString && StringComparer.OrdinalIgnoreCase.Equals(x: this._value, y: otherResourceString._value);
        }
    }
}