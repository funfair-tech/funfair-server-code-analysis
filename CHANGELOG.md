# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELEASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
- FF-1429 - Updated FunFair.Test.Common to 5.9.1.1665
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 17.1.0
- FF-1429 - Updated Meziantou.Analyzer to 1.0.695
### Removed
### Deployment Changes

<!--
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
## [5.8.1] - 2022-02-09
### Changed
- FF-1429 - Updated FunFair.Test.Common to 5.9.0.1658
- FF-3881 - Updated DotNet SDK to 6.0.102

## [5.8.0] - 2022-02-07
### Added
- Dotnet 6.0 Fixes
- Added FFS0041 - Do not use System.Console in test assemblies.
### Changed
- FF-1429 - Updated SmartAnalyzers.CSharpExtensions.Annotations to 4.2.1
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.30
- FF-1429 - Updated SecurityCodeScan.VS2019 to 5.6.0
- FF-1429 - Updated FunFair.Test.Common to 5.7.2.1514
- FF-3881 - Updated DotNet SDK to 6.0.101
- FF-1429 - Updated Philips.CodeAnalysis.DuplicateCodeAnalyzer to 1.1.6
- FF-1429 - Updated Roslynator.Analyzers to 4.0.2
- FF-1429 - Updated coverlet to 3.1.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.35.0.42613
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.34
- FF-1429 - Updated Meziantou.Analyzer to 1.0.694
- FF-1429 - Updated FunFair.Test.Common to 5.8.4.1638
- FF-1429 - Updated coverlet to 3.1.2
- FF-1429 - Updated FunFair.Test.Common to 5.8.5.1649

## [5.7.3] - 2021-12-02
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.32.0.39516
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 4.0.1
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.28
- FF-1429 - Updated Roslynator.Analyzers to 3.3.0
- FF-3856 - Updated to dotnet 6.0
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.29

## [5.7.0] - 2021-11-15
### Added
- FFS0040 - Type should be in a file with the same name as the type
### Changed
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 17.0.64
- FF-1429 - Updated to Dotnet SDK 5.0.403
- FF-1429 - Updated FunFair.Test.Common to 5.6.4.1351
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 4.0.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.31.0.39249
- FF-1429 - Updated NSubstitute.Analyzers.CSharp to 1.0.15
### Removed
- Unused dependencies

## [5.6.0] - 2021-11-01
### Added
- FFS0039 - Only one type name should be defined per file
### Changed
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.27
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 17.0.63
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.30.0.37606
- FF-1429 - Updated Microsoft.CodeAnalysis to 3.3.3
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 17.0.0

## [5.5.0] - 2021-09-15
### Added
- Check that records have DebuggerDisplay attribute on them.
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.29.0.36737

## [5.4.0] - 2021-08-25
### Added
- Banned Guid.Parse as should use new Guid or Guid.TryParse instead
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.27.0.35380
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.11.0
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.11.0
- FF-1429 - Updated Roslynator.Analyzers to 3.2.2

## [5.3.0] - 2021-07-27
### Added
- Check that test classes do not define mutable fields.
- Check that test classes do not define mutable properties

## [5.2.5] - 2021-07-26
### Added
- FF-3697 - Explicitly ban .AddJsonFile where 'reloadOnChange' parameter is true
### Changed
- FF-1429 - Updated coverlet to 3.1.0
- FF-1429 - Updated FunFair.Test.Common to 5.5.0.1195

## [5.2.4] - 2021-07-19
### Added
- Check if built-in methods are used for NonBlocking.ConcurrentDictionary<,> and force usage of extension methods under FunFair.Common.Extensions.ConcurrentDictionaryExtensions
### Changed
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.26

## [5.2.3] - 2021-07-13
### Fixed
- Check that symbol has ContainingNamespace
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.26.0.34506

## [5.2.2] - 2021-07-12
### Added
- Check if NonBlocking dictionary is used

## [5.2.1] - 2021-07-04
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.23.0.32424
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.10.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.10.56
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.24.0.32949
- FF-1429 - Updated Roslynator.Analyzers to 3.2.0
- FF-1429 - Updated Microsoft.CodeAnalysis to 3.10.0
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.25.0.33663
- FF-1429 - Updated FunFair.Test.Common to 5.4.0.1031

## [5.2.0] - 2021-05-13
### Added
- Checks on classes that are derived from MockBase
### Changed
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.25
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.9.0
- FF-1429 - Updated Microsoft.VisualStudio.Threading.Analyzers to 16.9.60
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.9.4
- FF-1429 - Updated FunFair.Test.Common to 5.3.0.920
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.22.0.31243

## [5.1.0] - 2021-02-08
### Added
- Added check that SuppressMessage contains a justification
- Checks that records are sealed
### Changed
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.16.0.25740
- FF-1429 - Updated FunFair.Test.Common to 5.0.0.735
- FF-1429 - Updated AsyncFixer to 1.4.0
- FF-1429 - Updated FunFair.Test.Common to 5.1.0.784
- FF-1429 - Updated FunFair.Test.Common to 5.1.1.792
- FF-1429 - Updated AsyncFixer to 1.4.1
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.17.0.26580
- FF-1429 - Updated AsyncFixer to 1.5.1
- FF-1429 - Updated Roslynator.Analyzers to 3.1.0
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.18.0.27296

## [5.0.0] - 2020-12-18
### Changed
- FF-3198 - Update all the .NET components to .NET 5.0.101

## [1.15.0] - 2020-09-14
### Changed
- FF-1429 - Updated Microsoft.CodeAnalysis.CSharp.Workspaces to 3.8.0
- FF-1429 - Updated NSubstitute.Analyzers.CSharp to 1.0.14
- FF-1429 - Updated Microsoft.Extensions to 5.0.0
- FF-1429 - Updated TeamCity.VSTest.TestAdapter to 1.0.23
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
- FF-1429 - Updated SonarAnalyzer.CSharp to 8.15.0.24505
- FF-1429 - Updated Microsoft.NET.Test.Sdk to 16.8.3
- FF-1429 - Updated Microsoft.CodeAnalysis.Analyzers to 3.3.2

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