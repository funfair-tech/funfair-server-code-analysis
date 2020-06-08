# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- FF-1429 - Updated FunFair.Test.Common to 1.7.1.350
- FF-1429 - Updated xunit.runner.visualstudio to 2.4.2
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.8.0.18411
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.6.0
- FF-1429 - Updated AsyncFixer to 1.3.0
- FF-1429 - Updated AsyncFixer to 1.1.8
- FF-1429 - Updated FunFair.Test.Common to 1.7.0.343
- FF-2386 - Update all the .NET components to .NET Core 3.1.202
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [1.4.1] - 2019-04-23
### Removed
- Whitelist for #pragma warning for nullable errors
## [1.4.0] - 2019-04-22
### Added
- Check to make sure unit tests and integraton tests derive from FunFair.Test.Common.TestBase.

## [1.3.0] - 2019-04-19
### Added
- Checks for structs that are not marked as read-only.
- Checks for classes that are not marked as static, sealed or abstract.

## [1.2.1] - 2019-03-30
- Fixed prohibition of ISqlServerDatabase.QueryArbitrarySqlAsync<>

## [1.2.0] - 2019-03-30
### Changed
- FF-2127 - references dotnet core 3.1.201

## [1.1.0] - 2019-02-18
### Added
- FF-1848 - Prohibition of XUnit Assert.True/Assert.False without message

## [1.0.5] - 2019-02-07
- Fixed Prohibition of non white-listed #pragma warning disables where sometimes it didn't actually prohibit
- Updated code analysis dependencies

## [1.0.4] - 2019-01-24
- Prohibition of non white-listed #pragma warning disables

## [1.0.3] - 2019-01-20
- Check for FunFair.Common.Data.ISqlServerDatabase::QueryArbitrarySqlAsync

### Added
## [1.0.2] - 2019-01-15
### Added
- Check for FunFair.Common.Data.ISqlServerDatabase::ExecuteArbitrarySqlAsync

## [1.0.1] - 2019-01-09
### Changed
- Changed the code analysis package to use .net standard 2.0 rather than 2.1 as VS2019 is incapable of running it!

## [1.0.0] - 2019-12-18
### Added
- Banned DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTimeOffset.Now and DateTimeOffset.UtcNow pointing to use DateTimeSource instead







