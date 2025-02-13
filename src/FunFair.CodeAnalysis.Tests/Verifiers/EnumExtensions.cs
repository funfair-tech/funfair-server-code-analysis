using System.Diagnostics.CodeAnalysis;
using Credfeto.Enumeration.Source.Generation.Attributes;
using Microsoft.CodeAnalysis;

namespace FunFair.CodeAnalysis.Tests.Verifiers;

[EnumText(typeof(DiagnosticSeverity))]
[SuppressMessage(
    category: "ReSharper",
    checkId: "PartialTypeWithSinglePart",
    Justification = "Needed for generated code"
)]
internal static partial class EnumExtensions
{
    // defined in other files
}
