using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage(
    category: "Philips.CodeAnalysis.MaintainabilityAnalyzers",
    checkId: "PH2140: Avoid ExcludeFromCodeCoverage",
    Justification = "This is a unit test assembly - no need for coverage of the test code itself"
)]
[assembly: ExcludeFromCodeCoverage]

[assembly: SuppressMessage(
    category: "Philips.CodeAnalysis.DuplicateCodeAnalyzer",
    checkId: "PH2071:DuplicateCodeDetection",
    Justification = "Test code"
)]
