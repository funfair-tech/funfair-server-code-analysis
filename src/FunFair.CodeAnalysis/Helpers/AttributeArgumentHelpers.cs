using System;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace FunFair.CodeAnalysis.Helpers;

internal static class AttributeArgumentHelpers
{
    public static string? GetStringArgument(
        AttributeSyntax attributeSyntax,
        string argumentName,
        int position,
        SemanticModel semanticModel,
        CancellationToken cancellationToken
    )
    {
        if (attributeSyntax.ArgumentList is null)
        {
            return null;
        }

        AttributeArgumentSyntax? named = attributeSyntax.ArgumentList.Arguments.FirstOrDefault(a =>
            StringComparer.Ordinal.Equals(x: a.NameColon?.Name.Identifier.Text, y: argumentName)
        );

        if (named is not null)
        {
            Optional<object> namedValue = semanticModel.GetConstantValue(
                expression: named.Expression,
                cancellationToken: cancellationToken
            );

            if (namedValue.HasValue && namedValue.Value is string namedStr)
            {
                return namedStr;
            }
        }

        AttributeArgumentSyntax? positional = attributeSyntax
            .ArgumentList.Arguments.Where(a => a.NameColon is null && a.NameEquals is null)
            .Skip(position)
            .FirstOrDefault();

        if (positional is not null)
        {
            Optional<object> positionalValue = semanticModel.GetConstantValue(
                expression: positional.Expression,
                cancellationToken: cancellationToken
            );

            if (positionalValue.HasValue && positionalValue.Value is string positionalStr)
            {
                return positionalStr;
            }
        }

        return null;
    }
}
