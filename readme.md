# dotnet-releaser [![Build Status](https://github.com/xoofx/dotnet-releaser/workflows/ci/badge.svg?branch=main)](https://github.com/xoofx/dotnet-releaser/actions) [![NuGet](https://img.shields.io/nuget/v/dotnet-releaser.svg)](https://www.nuget.org/packages/dotnet-releaser/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/xoofx/dotnet-releaser/main/img/dotnet-releaser.png">

`dotnet-releaser` is a all-in-one command line tool that fully automates the release cycle of your .NET libraries and applications to NuGet and GitHub by **building**, **testing**, **running coverage**, **cross-compiling**, **packaging**, **creating release notes from PR/commits** and **publishing**.

In practice, `dotnet-releaser` will automate the build and publish process of your .NET libraries and applications by wrapping:

- `dotnet build` with potentially multiple solutions
- `dotnet test`
  - Plus the automatic support for coverage.
- `dotnet pack` for creating NuGet packages
- `dotnet publish` that can automatically cross-compile to 9+ CPU/OS platforms.
  - And create additionally, by default, multiple packages (zip, debian, rpm...) to distribute your app
- `dotnet nuget push` to publish your package to a NuGet registry
- [Pretty changelog](https://github.com/xoofx/dotnet-releaser/blob/main/doc/changelog_user_guide.md#11-overview) creation from pull-requests and commits.
- Create and upload the changelog and all the packages packed to your GitHub repository associated with the release tag.

![overview](https://raw.githubusercontent.com/xoofx/dotnet-releaser/main/doc/overview.drawio.svg)

## Features

- **Very simple to use, configure and [integrate into your GitHub Action CI](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md#3-adding-dotnet-releaser-to-your-ci-on-github)**
- **Build** and **tests** your .NET libraries and applications from **multiple solutions**.
- Add automatic **coverage** support via [coverlet](https://github.com/coverlet-coverage/coverlet) with your tests.
- **Cross-compile** your .NET 6.0+ application to **9+ OS/CPU targets**.
- Create **zip archives**, **Linux packages** (debian, rpm) and **Homebrew taps**
- Allow to publish your **application as a service** (only `Systemd` for now for `deb` and `rpm` packages).
- **Create and publish beautiful release notes** by extracting the information directly from pull-requests and commits, while offering [customizable templates](https://github.com/xoofx/dotnet-releaser/blob/main/doc/changelog_user_guide.md).
- **Publish all artifacts** to **NuGet** and **GitHub**
- Can be used to build/tests/package/publish locally or from GitHub Action using the same command.

## Defaults

By default, `dotnet-releaser` will:

- **Build your project/solution** in Release 
- **Run tests** in Release
- **Create NuGet packages** for libraries and your application (packed as a .NET global tool)
- **Create application packages** for any packable application in your project:
  - `[win-x64]` with `[zip]` package            
  - `[win-arm]` with `[zip]` package            
  - `[win-arm64]` with `[zip]` package          
  - `[linux-x64]` with `[deb, tar]` packages    
  - `[linux-arm]` with `[deb, tar]` packages    
  - `[linux-arm64]` with `[deb, tar]` packages  
  - `[rhel-x64]` with `[rpm, tar]` packages     
  - `[osx-x64]` with `[tar]` package            
  - `[osx-arm64]` with `[tar]` package          
- **Publish your application as a global tool to NuGet**
- **Upload all the package artifacts and your changelog to GitHub** on the tag associated with your package version (e.g your package is `1.0.0`, it will try to find a git tag `v1.0.0` or `1.0.0`).
- **Create a Homebrew repository and formula**  (e.g `user_or_org/homebrew-your-app-name`) for all the tar files associated with the targets for Linux and MacOS.

> Any of these steps can be configured or even entirely disabled easily from a config file.
> See the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md) on how to setup this differently for your application.
## Getting Started

- Create a `dotnet-releaser.toml` at the same level you have your .NET solution. Most projects won't need more than this kind of configuration:
  ```toml
  [msbuild]
  project = "Tonlyn.sln"
  [github]
  user = "xoofx"
  repo = "Tomlyn"
  ```
- Install `dotnet-releaser` as a global .NET tool. Verify or update to version accordingly.
  ```
  dotnet tool install --global dotnet-releaser --version "0.2.*"
  ```
- If you want to try a full build locally:
  ```
  dotnet-releaser build --force dotnet-releaser.toml
  ```

See the user guide below for further details on how to use `dotnet-releaser`.

## User Guide

For more details on how to use `dotnet-releaser`, please visit the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md).
## License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## Who is using `dotnet-releaser`?

It's brand new, so only the author for now! :D

You can see it's usage on the project [grpc-curl here](https://github.com/xoofx/grpc-curl/releases/tag/1.3.2).

## Credits

`dotnet-releaser` is a wrapper around many amazing OSS libraries:

- [dotnet-packaging](https://github.com/quamotion/dotnet-packaging) by using their NuGet [Packaging.Targets](https://www.nuget.org/packages/Packaging.Targets) to hook package creation into MSBuild user's project.
- [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) for handling parsing command line arguments
- [Microsoft.Extensions.Logging](https://github.com/dotnet/runtime/) for logging to the console.
- [MsBuildPipeLogger](https://github.com/daveaglick/MsBuildPipeLogger) for interacting with MSBuild structured output.
- [Octokit.NET](https://github.com/octokit/octokit.net) for interacting with GitHub.
- [Tomlyn](https://github.com/xoofx/Tomlyn) for parsing the TOML configuration file.
- [CliWrap](https://github.com/Tyrrrz/CliWrap) to easily wrap and launch executables.
- [Spectre.Console](https://github.com/spectreconsole/spectre.console) for generating pretty logs and table reports.
- [Scriban](https://github.com/scriban/scriban) used for text templating of the changelog/release notes.
- [DotNet.Glob](https://github.com/dazinator/DotNet.Glob) used by changelog filtering on files.
## Author

Alexandre Mutel aka [xoofx](http://xoofx.com).
