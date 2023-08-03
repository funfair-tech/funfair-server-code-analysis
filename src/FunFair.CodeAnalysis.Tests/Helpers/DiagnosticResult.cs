using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Helpers;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Test code")]
public readonly struct DiagnosticResult
{
    public DiagnosticResultLocation[] Locations { get; init; }

    public DiagnosticSeverity Severity { get; init; }

    public string Id { get; init; }

    public string Message { get; init; }

    public int Line =>
        this.Locations.Length > 0
            ? this.Locations[0].Line
            : -1;

    public int Column =>
        this.Locations.Length > 0
            ? this.Locations[0].Column
            : -1;
}