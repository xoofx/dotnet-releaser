# dotnet-releaser [![Build Status](https://github.com/xoofx/dotnet-releaser/workflows/ci/badge.svg?branch=main)](https://github.com/xoofx/dotnet-releaser/actions) [![NuGet](https://img.shields.io/nuget/v/dotnet-releaser.svg)](https://www.nuget.org/packages/dotnet-releaser/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/xoofx/dotnet-releaser/main/img/dotnet-releaser.png">

`dotnet-releaser` is a all-in-one command line tool that fully automates the release cycle of your .NET libraries and applications to NuGet and GitHub by **building**, **testing**, **running coverage**, **cross-compiling**, **packaging**, **creating release notes from PR/commits** and **publishing**.

## Features

- **Very simple to use, configure and [integrate into your GitHub Action CI](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md#3-adding-dotnet-releaser-to-your-ci-on-github)**
- **Build** and **tests** your .NET libraries and applications from **multiple solutions**.
- Add automatic **coverage** support via [coverlet](https://github.com/coverlet-coverage/coverlet) with your tests.
- **Cross-compile** your .NET 6.0+ application to **9+ OS/CPU targets**.
- Create **zip archives**, **Linux packages** (debian, rpm) and **Homebrew taps**
- Allow to publish your **application as a service** (only `Systemd` for now for `deb` and `rpm` packages).
- **Create and publish beautiful release notes** by extracting the information directly from pull-requests and commits, while offering [customizable templates](https://github.com/xoofx/dotnet-releaser/blob/main/doc/changelog_user_guide.md).
- **Publish all artifacts** to **NuGet** and **GitHub**
- Integrate `dotnet-releaser` easily in [your GitHub Action workflow](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md#3-adding-dotnet-releaser-to-your-ci-on-github).

## Defaults

By default, `dotnet-releaser` will package your .NET libraries and applications:

- NuGet package (packed as a .NET global tool)
- `[win-x64]` with `[zip]` package            
- `[win-arm]` with `[zip]` package            
- `[win-arm64]` with `[zip]` package          
- `[linux-x64]` with `[deb, tar]` packages    
- `[linux-arm]` with `[deb, tar]` packages    
- `[linux-arm64]` with `[deb, tar]` packages  
- `[rhel-x64]` with `[rpm, tar]` packages     
- `[osx-x64]` with `[tar]` package            
- `[osx-arm64]` with `[tar]` package          

When publishing, `dotnet-releaser` will automatically:

- **Publish your application as a global tool to NuGet**
- **Upload all the package artifacts and your changelog to GitHub** on the tag associated with your package version (e.g your package is `1.0.0`, it will try to find a git tag `v1.0.0` or `1.0.0`).
- **Create a Homebrew repository and formula**  (e.g `user_or_org/homebrew-your-app-name`) for all the tar files associated with the targets for Linux and MacOS.

See the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md) on how to setup this differently for your application.
## User Guide

For more details on how to use `dotnet-releaser`, please visit the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md).
## License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## Who is using `dotnet-releaser`?

It's brand new, so only the author for now! :D

You can see it's usage on the project [grpc-curl here](https://github.com/xoofx/grpc-curl/releases/tag/1.3.2).

## Credits

`dotnet-releaser` is just a modest wrapper around many amazing OSS libraries:

- [dotnet-packaging](https://github.com/quamotion/dotnet-packaging) by using their NuGet [Packaging.Targets](https://www.nuget.org/packages/Packaging.Targets) to hook package creation into MSBuild user's project.
- [CommandLineUtils](https://github.com/natemcmaster/CommandLineUtils) for handling parsing command line arguments
- [Microsoft.Extensions.Logging](https://github.com/dotnet/runtime/) for logging to the console.
- [MSBuildStructuredLog](https://github.com/KirillOsenkov/MSBuildStructuredLog) for interacting with MSBuild structured output.
- [Octokit.NET](https://github.com/octokit/octokit.net) for interacting with GitHub.
- [Tomlyn](https://github.com/xoofx/Tomlyn) for parsing the TOML configuration file.
- [CliWrap](https://github.com/Tyrrrz/CliWrap) to easily wrap and launch executables.
## Author

Alexandre Mutel aka [xoofx](http://xoofx.com).
