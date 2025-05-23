using System;
using System.Diagnostics.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Helpers;

[SuppressMessage(
    category: "Microsoft.Performance",
    checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes",
    Justification = "Test code"
)]
public readonly record struct DiagnosticResultLocation
{
    public DiagnosticResultLocation(string path, int line, int column)
    {
        if (line < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(line), message: "line must be >= -1");
        }

        if (column < -1)
        {
            throw new ArgumentOutOfRangeException(nameof(column), message: "column must be >= -1");
        }

        this.Path = path;
        this.Line = line;
        this.Column = column;
    }

    public string Path { get; }

    public int Line { get; }

    public int Column { get; }
}
