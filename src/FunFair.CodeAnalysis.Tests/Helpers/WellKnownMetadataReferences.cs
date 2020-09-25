using System.Net;
using System.Text.Json;
using System.Threading;
using FunFair.Test.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

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

        public static readonly MetadataReference HttpContext = MetadataReference.CreateFromFile(typeof(DefaultHttpContext).Assembly.Location);

        public static readonly MetadataReference IpAddress = MetadataReference.CreateFromFile(typeof(IPAddress).Assembly.Location);

        public static readonly MetadataReference ConnectionInfo = MetadataReference.CreateFromFile(typeof(ConnectionInfo).Assembly.Location);
    }
}