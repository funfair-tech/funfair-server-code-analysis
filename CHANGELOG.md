# Changelog
All notable changes to this project will be documented in this file.

<!--
Please ADD ALL Changes to the UNRELASED SECTION and not a specific release
-->

## [Unreleased]
### Added
### Fixed
### Changed
### Removed
### Deployment Changes

<!-- 
Releases that have at least been deployed to staging, BUT NOT necessarily released to live.  Changes should be moved from [Unreleased] into here as they are merged into the appropriate release branch
-->
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
