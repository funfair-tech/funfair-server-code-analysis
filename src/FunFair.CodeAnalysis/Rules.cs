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
        public const string RuleDontUseArbitrarySqlForQueries = @"FFS0007";
        public const string RuleDontDisableWarnings = @"FFS0008";
        public const string RuleDontUseAssertTrueWithoutMessage = @"FFS0009";
        public const string RuleDontUseAssertFalseWithoutMessage = @"FFS0010";
        public const string RuleStructsShouldBeReadOnly = @"FFS0011";
        public const string RuleClassesShouldBeStaticSealedOrAbstract = @"FFS0012";
        public const string RuleTestClassesShouldBeStaticSealedOrAbstractDerivedFromTestBase = @"FFS0013";
        public const string RuleDontUseJsonSerializerWithoutJsonOptions = @"FFS0014";
        public const string RuleDontUseJsonDeserializerWithoutJsonOptions = @"FFS0015";
        public const string RuleMustPassParameterNameToArgumentExceptions = @"FFS0016";
    }
}