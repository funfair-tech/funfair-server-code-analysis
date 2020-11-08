# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELASED SECTION and not a specific release
-->

## [Unreleased]
### Added

### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->

## [1.15.0] - 2020-09-14
### Changed
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.8.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.8.55
- FF-1429 - Updated FunFair.Test.Common to 1.14.0.633
- FF-1429 - Updated Microsoft.CodeAnalysis.Analyzers to 3.3.1
- FF-1429 - Updated Microsoft.CodeAnalysis.FxCopAnalyzers to 3.3.1
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.8.51
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.8.50
- FF-1429 - Updated FunFair.Test.Common to 1.14.0.607
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.14.0.22654
- FF-2930 - Updated to .net core 3.1.403

## [1.14.0] - 2020-09-27
### Added
- FF-2866 - Prohibit querying RemoteIpAddress directly on connection.

## [1.13.0] - 2020-09-25
### Added
- FF-2885 - Checks for ILogger<T> being misused (e.g. not using the correct category)

## [1.12.0] - 2020-09-23
### Added
- FF-2876 - Prohibition of #nullable disable as nullable should be enabled globally on a per project level.
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.13.1.21947
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.13.0.21683
- FF-1429 - Updated FunFair.Test.Common to 1.13.0.520
- FF-1429 - Updated FunFair.Test.Common to 1.12.0.508

## [1.11.0] - 2020-09-09
### Changed
- FF-2830 - Update all the .NET components to .NET Core 3.1.402
- FF-1429 - Updated FunFair.Test.Common to 1.11.3.492
- FF-1429 - Updated Roslynator.Analyzers to 3.0.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.7.56
- FF-1429 - Updated FunFair.Test.Common to 1.11.3.478

## [1.10.0] - 2020-09-02
### Changed
- FF-2802 - Prohibit Received(0) in tests
- FF-1429 - Updated FunFair.Test.Common to 1.11.2.471
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.7.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.12.0.21095
- FF-1429 - Updated FunFair.Test.Common to 1.11.1.466
- FF-1429 - Updated FunFair.Test.Common to 1.11.0.461

## [1.9.0] - 2020-08-12
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.11.0.20529
- FF-1429 - Updated Microsoft.CodeAnalysis.Analyzers to 3.3.0
- FF-1429 - Updated Microsoft.CodeAnalysis.FxCopAnalyzers to 3.3.0
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.7.0
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.7.0
- FF-1429 - Updated FunFair.Test.Common to 1.10.1.439
- FF-1429 - Updated xunit.runner.visualstudio to 2.4.3
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.7.54
- FF-1429 - Updated FunFair.Test.Common to 1.10.1.430
- FF-1429 - Updated FunFair.Test.Common to 1.10.0.421
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.10.0.19839
- FF-1429 - Updated FunFair.Test.Common to 1.9.0.413
- FF-2759 - Updated to .net core 3.1.401

## [1.8.0] - 2020-07-20
### Changed
- FF-1429 - Updated FunFair.Test.Common to 1.8.2.400
- FF-2652 - Update all the .NET components to .NET Core 3.1.302
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.22

## [1.7.2] - 2020-07-08
### Added
- FF-2616 - Check compiler errors in unit test source code
- FF-2623 - Check parameter ordering to make logger parameters last or next to last.
### Changed
- FF-1429 - Updated FunFair.Test.Common to 1.8.1.387
- FF-1429 - Updated FunFair.Test.Common to 1.8.1.386

## [1.7.1] - 2020-07-07
### Added
- FF-2617 - ILogger parameters should be called 'logger'

## [1.7.0] - 2020-07-06
### Added
- FF-2351 - Prohibit use of NSubstitute.Received() without a count of items
### Fixed
- FF-2413 - Amount of required arguments for DeserializeAsync

## [1.6.0] - 2020-07-01
### Added
- FF-2413 - JsonSerializer serialize and deserialize rules
- FF-2590 - Explicit checks for ArgumentExceptions that they have the parameter name passed
- FF-2591 - Re-Throwing Exception as new exception should pass inner exception
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.9.0.19135
- FF-1429 - Updated FunFair.Test.Common to 1.8.0.360

## [1.5.0] - 2020-06-18
### Changed
- FF-2488 - Updated packages and global.json to net core 3.1.301
- FF-1429 - Updated FunFair.Test.Common to 1.7.1.350
- FF-1429 - Updated xunit.runner.visualstudio to 2.4.2
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.8.0.18411
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.6.0
- FF-1429 - Updated AsyncFixer to 1.3.0
- FF-1429 - Updated AsyncFixer to 1.1.8
- FF-1429 - Updated FunFair.Test.Common to 1.7.0.343
- FF-2386 - Update all the .NET components to .NET Core 3.1.202

## [1.4.1] - 2020-04-23
### Removed
- Whitelist for #pragma warning for nullable errors
## [1.4.0] - 2020-04-22
### Added
- Check to make sure unit tests and integraton tests derive from FunFair.Test.Common.TestBase.

## [1.3.0] - 2020-04-19
### Added
- Checks for structs that are not marked as read-only.
- Checks for classes that are not marked as static, sealed or abstract.

## [1.2.1] - 2020-03-30
- Fixed prohibition of ISqlServerDatabase.QueryArbitrarySqlAsync<>

## [1.2.0] - 2020-03-30
### Changed
- FF-2127 - references dotnet core 3.1.201

## [1.1.0] - 2020-02-18
### Added
- FF-1848 - Prohibition of XUnit Assert.True/Assert.False without message

## [1.0.5] - 2020-02-07
- Fixed Prohibition of non white-listed #pragma warning disables where sometimes it didn't actually prohibit
- Updated code analysis dependencies

## [1.0.4] - 2020-01-24
- Prohibition of non white-listed #pragma warning disables

## [1.0.3] - 2020-01-20
- Check for FunFair.Common.Data.ISqlServerDatabase::QueryArbitrarySqlAsync

### Added
## [1.0.2] - 2020-01-15
### Added
- Check for FunFair.Common.Data.ISqlServerDatabase::ExecuteArbitrarySqlAsync

## [1.0.1] - 2020-01-09
### Changed
- Changed the code analysis package to use .net standard 2.0 rather than 2.1 as VS2019 is incapable of running it!

## [1.0.0] - 2019-12-18
### Added
- Banned DateTime.Now, DateTime.UtcNow, DateTime.Today, DateTimeOffset.Now and DateTimeOffset.UtcNow pointing to use DateTimeSource instead
