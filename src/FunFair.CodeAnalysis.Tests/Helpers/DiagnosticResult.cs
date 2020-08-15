using System;
using System.Diagnostics.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Helpers
{
    /// <summary>
    ///     Location where the diagnostic appears, as determined by path, line number, and column number.
    /// </summary>
    [SuppressMessage(category: "Microsoft.Performance", checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Test code")]
    public struct DiagnosticResultLocation
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

    /// <summary>
    ///     Struct that stores information about a Diagnostic appearing in a source
    /// </summary>
    [SuppressMessage(category: "Microsoft.Performance", checkId: "CA1815:OverrideEqualsAndOperatorEqualsOnValueTypes", Justification = "Test code")]
    public struct DiagnosticResult
    {
        private DiagnosticResultLocation[] _locations;

        public DiagnosticResultLocation[] Locations
        {
            get
            {
                if (this._locations == null)
                {
                    this._locations = Array.Empty<DiagnosticResultLocation>();
                }

                return this._locations;
            }

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