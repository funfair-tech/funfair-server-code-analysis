# funfair-server-codeanalysis
Static Code analysis Repo for FunFair Server dotnet projects. 

|Code|Meaning|
|----|-------|
|FFS0001|Avoid using ``DateTime.Now`` - Use ``IDateTimeSource.UtcNow()``|
|FFS0002|Avoid using ``DateTime.UtcNow`` - Use ``IDateTimeSource.UtcNow()``|
|FFS0003|Avoid using ``DateTime.Today`` - Use ``IDateTimeSource.UtcNow().Date``|
|FFS0004|Avoid using ``DateTimeOffset.Now`` - Use ``IDateTimeSource.UtcNow()``|
|FFS0005|Avoid using ``DateTimeOffset.UtcNow`` - Use ``IDateTimeSource.UtcNow()``|
|FFS0006|Avoid using arbitrary SQL for updates|
|FFS0007|Avoid using arbitrary SQL for queries|
|FFS0008|Do not disable warnings|
|FFS0009|Do not use ``Assert.True`` without specifying a message|
|FFS0010|Do not use ``Assert.False`` without specifying a message|
|FFS0011|Make structs ``readonly``|
|FFS0012|Classes should be ``static``, ``sealed`` or ``abstract``|
|FFS0013|Test Classes should be  ``sealed`` or ``abstract`` and derived from ``TestBase``|
|FFS0014|Do not use ``JsonSerialiser`` without specifying ``JsonOptions``|
|FFS0015|Do not use ``JsonDeserialiser`` without specifying ``JsonOptions``|
|FFS0016|Pass parameter name to ``ArgumentExceptions``|
|FFS0017|Pass inner exception to exceptions thrown in catch block|
|FFS0018|Don't use NSubstitute's Received() without specifying the number of calls|
|FFS0019|``ILogger`` parameters should be called logger|
|FFS0020|Parameters should be in a specified order|
|FFS0021|Don't use NSubstitute's ``Received(0)`` - use ``DidNotReceive()`` instead|
|FFS0022|Don't configure nullable in code - should be a project level.|
|FFS0023|Logger parameters on base classes should be ``ILogger`` not ``ILogger<ClassName>``|
|FFS0024|Logger parameters on leaf classes should be ``ILogger<ClassName>`` not ``ILogger``|
|FFS0025|Mismatch of generic type|
|FFS0026|Do not read IPAddress from Connection - use an abstraction|
|FFS0027|``SuppressMessage`` must specify a justification|
|FFS0028|Records should be ``sealed``|
|FFS0029|Classes derived from ``MockBase<T>`` should be ``internal``|
|FFS0030|Classes derived from ``MockBase<T>`` should be ``sealed``|
|FFS0031|Avoid using ``System.Collections.Concurrent.ConcurrentDictionary<,>`` - Use ``NonBlocking.ConcurrentDictionary<,>``|
|FFS0032|Avoid using ``NonBlocing.ConcurrentDictionary<,>.AddOrUpdate`` - Use ``FunFair.Common.Extensions.ConcurrentDictionaryExtensions.AddOrUpdate``|
|FFS0033|Avoid using ``NonBlocing.ConcurrentDictionary<,>.GetOrAdd`` - Use ``FunFair.Common.Extensions.ConcurrentDictionaryExtensions.GetOrAdd``|
|FFS0034|Avoid using ``Microsoft.Extensions.Configuration.ConfigurationBuilder.AddJsonFile`` with reload set to true|
|FFS0035|Checks that test classes do not define mutable fields|
|FFS0036|Checks that test classes do not define mutable properties|
|FFS0037|Checks that ``Guid.Parse`` is not used and that ``new Guid`` or ``Guid.TryParse`` is used instead|
|FFS0038|Records should have ``DebuggerDisplay`` attribute on them.|
|FFS0039|Only one type name should be defined per file. Note ``class T`` and ``class T<T1>`` are considered to be one type as they share the same name.|

## Changelog

View [changelog](CHANGELOG.md)


[CHANGELOG]: ./CHANGELOG.md