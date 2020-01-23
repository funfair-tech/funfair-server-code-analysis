using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis
{
    internal static class RuleHelpers
    {
        public static DiagnosticDescriptor CreateRule(string code, string category, string title, string message)
        {
            LiteralString translatableTitle = new LiteralString(title);
            LiteralString translatableMessage = new LiteralString(message);

            return new DiagnosticDescriptor(code, translatableTitle, translatableMessage, category, DiagnosticSeverity.Error, isEnabledByDefault: true, translatableMessage);
        }
    }
}