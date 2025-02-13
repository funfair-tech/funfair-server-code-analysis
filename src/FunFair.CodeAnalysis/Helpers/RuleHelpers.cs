using System;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Helpers;

internal static class RuleHelpers
{
    public static DiagnosticDescriptor CreateRule(
        string code,
        string category,
        string title,
        string message
    )
    {
        LiteralString translatableTitle = new(title);
        LiteralString translatableMessage = UseTitleForMessage(
            title: title,
            message: message,
            translatableTitle: translatableTitle
        );

        return new(
            id: code,
            title: translatableTitle,
            messageFormat: translatableMessage,
            category: category,
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: translatableMessage
        );
    }

    private static LiteralString UseTitleForMessage(
        string title,
        string message,
        LiteralString translatableTitle
    )
    {
        return StringComparer.Ordinal.Equals(x: message, y: title)
            ? translatableTitle
            : new(message);
    }
}
