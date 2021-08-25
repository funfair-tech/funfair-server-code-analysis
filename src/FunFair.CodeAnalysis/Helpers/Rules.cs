namespace FunFair.CodeAnalysis.Helpers
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
        public const string RuleMustPassInterExceptionToExceptionsThrownInCatchBlock = @"FFS0017";
        public const string RuleDontUseSubstituteReceivedWithoutAmountOfCalls = @"FFS0018";
        public const string RuleLoggerParametersShouldBeCalledLogger = @"FFS0019";
        public const string RuleParametersShouldBeInOrder = @"FFS0020";
        public const string RuleDontUseSubstituteReceivedWithZeroNumberOfCalls = @"FFS0021";
        public const string RuleDontConfigureNullableInCode = @"FFS0022";
        public const string LoggerParametersOnBaseClassesShouldNotUseGenericLoggerCategory = @"FFS0023";
        public const string LoggerParametersOnLeafClassesShouldUseGenericLoggerCategory = @"FFS0024";
        public const string GenericTypeMissMatch = @"FFS0025";
        public const string RuleDontReadRemoteIpAddressDirectlyFromConnection = @"FFS0026";
        public const string RuleSuppressMessageMustHaveJustification = @"FFS0027";
        public const string RuleRecordsShouldBeSealed = @"FFS0028";
        public const string MockBaseClassInstancesMustBeInternal = @"FFS0029";
        public const string MockBaseClassInstancesMustBeSealed = @"FFS0030";
        public const string RuleDontUseConcurrentDictionary = @"FFS0031";
        public const string RuleDontUseBuildInAddOrUpdateConcurrentDictionary = @"FFS0032";
        public const string RuleDontUseBuildInGetOrAddConcurrentDictionary = @"FFS0033";
        public const string RuleDontUseConfigurationBuilderAddJsonFileWithReload = @"FFS0034";
        public const string RuleTestClassesShouldNotDefineMutableFields = @"FFS0035";
        public const string RuleTestClassesShouldNotDefineMutableProperties = @"FFS0036";
        public const string RuleDontUseGuidParse = @"FFS0037";
    }
}