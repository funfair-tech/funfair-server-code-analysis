using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Helpers;

[SuppressMessage(category: "Microsoft.Performance", checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Test code")]
public readonly record struct DiagnosticResult(IReadOnlyList<DiagnosticResultLocation> Locations, DiagnosticSeverity Severity, string Id, string Message)
{
    public int Line => this.Locations is not [] ? this.Locations[0].Line : -1;

    public int Column => this.Locations is not [] ? this.Locations[0].Column : -1;
}
