using System.Text.Json;
using System.Threading;

namespace FunFair.CodeAnalysis.Tests.Helpers
{
    internal static class WellKnownMetadataReferences
    {
        public static readonly MetadataReference GenericLogger = MetadataReference.CreateFromFile(typeof(ILogger<>).Assembly.Location);

        public static readonly MetadataReference Logger = MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location);

        public static readonly MetadataReference CancellationToken = MetadataReference.CreateFromFile(typeof(CancellationToken).Assembly.Location);

        public static readonly MetadataReference JsonSerializer = MetadataReference.CreateFromFile(typeof(JsonSerializer).Assembly.Location);

        public static readonly MetadataReference Substitute = MetadataReference.CreateFromFile(typeof(Substitute).Assembly.Location);

        public static readonly MetadataReference Assert = MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location);

        public static readonly MetadataReference Xunit = MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location);

        public static readonly MetadataReference XunitAbstractions = MetadataReference.CreateFromFile(typeof(ITestOutputHelper).Assembly.Location);

        public static readonly MetadataReference FunFairTestCommon = MetadataReference.CreateFromFile(typeof(TestBase).Assembly.Location);
    }
}