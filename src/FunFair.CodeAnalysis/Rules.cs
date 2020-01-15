namespace FunFair.CodeAnalysis
{
    internal static class Rules
    {
        public const string RuleDontUseDateTimeNow = @"FFS0001";
        public const string RuleDontUseDateTimeUtcNow = @"FFS0002";
        public const string RuleDontUseDateTimeToday = @"FFS0003";
        public const string RuleDontUseDateTimeOffsetNow = @"FFS0004";
        public const string RuleDontUseDateTimeOffsetUtcNow = @"FFS0005";
        public const string RuleDontUseArbitrarySql = @"FFS0006";
    }
}