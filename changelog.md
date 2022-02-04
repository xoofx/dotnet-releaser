# Changelog

## 0.1.11 (4 Feb 2022)
- Add support for creating users for service

## 0.1.10 (4 Feb 2022)
- Add suppport for specifying package dependencies for deb and rpm (#14)

## 0.1.9 (3 Feb 2022)
- Do not NuGet pack an app that is not IsPackable = true (#12)

## 0.1.8 (3 Feb 2022)
- Use MSBuild built-in binary logger instead of StructuredLogger

## 0.1.7 (3 Feb 2022)
- Update to latest StructuredLogger

## 0.1.6 (2 Feb 2022)
- Update to latest StructuredLogger
- Add support for application published as a service (only systemd for now).

## 0.1.5 (31 Jan 2022)
- Fix homebrew class name with capitalizing

## 0.1.4 (31 Jan 2022)
- Fix homebrew when app name has invalid characters for a class name in ruby
- Fix homebrew to copy all files from archive

## 0.1.3 (31 Jan 2022)

- Don't log an error if changelog.path is empty

## 0.1.2 (31 Jan 2022)

- Log output from NuGet push

## 0.1.1 (31 Jan 2022)

- Update StructuredLogger and CliWrap dependencies
- Use ArgumentBuilder with CliWrap
- Exit early on changelog error

## 0.1.0 (29 Jan 2022)

- Initial version