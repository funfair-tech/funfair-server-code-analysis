using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Helpers
{
    /// <summary>
    ///     Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    [SuppressMessage(category: "Microsoft.Performance", checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Test code")]
    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[]? _locations;

        public DiagnosticResultLocation[] Locations
        {
            get => this._locations ??= Array.Empty<DiagnosticResultLocation>();

            set => this._locations = value;
        }

        public DiagnosticSeverity Severity { get; set; }

        public string Id { get; set; }

        public string Message { get; set; }

        public string Path =>
            this.Locations.Length > 0
                ? this.Locations[0]
                      .Path
                : "";

        public int Line =>
            this.Locations.Length > 0
                ? this.Locations[0]
                      .Line
                : -1;

        public int Column =>
            this.Locations.Length > 0
                ? this.Locations[0]
                      .Column
                : -1;
    }
}