# dotnet-releaser [![ci](https://github.com/xoofx/dotnet-releaser/actions/workflows/ci.yml/badge.svg)](https://github.com/xoofx/dotnet-releaser/actions/workflows/ci.yml) [![NuGet](https://img.shields.io/nuget/v/dotnet-releaser.svg)](https://www.nuget.org/packages/dotnet-releaser/)

<img align="right" width="160px" height="160px" src="https://raw.githubusercontent.com/xoofx/dotnet-releaser/main/img/dotnet-releaser.png">

`dotnet-releaser` is an all-in-one command line tool that fully automates the release cycle of your .NET libraries and applications to NuGet and GitHub by **building**, **testing**, **running coverage**, **cross-compiling**, **packaging**, **creating release notes from PR/commits** and **publishing**.

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
- It can publish automatically the coverage results to a badge in a GitHub gist or to https://coveralls.io if your repository is created there.
- `dotnet-releaser` tool requires .NET 9.0 runtime to be installed.
  
![overview](https://raw.githubusercontent.com/xoofx/dotnet-releaser/main/doc/overview.drawio.svg)

## Features

- **Very simple to use, configure and [integrate into your GitHub Action CI](https://github.com/xoofx/dotnet-releaser/tree/main/doc#12-adding-dotnet-releaser-to-your-ci-on-github)**
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

- **Build your project(s)/solution(s)** in Release 
- **Run tests** in Release
- **Create NuGet packages** for libraries and applications (packed as a .NET global tool)
- **Create application packages** for any packable application in your project:
  | Platform                                | Packages         |
  |-----------------------------------------|------------------|
  | `win-x64`, `win-arm64`                  | `zip`
  | `linux-x64`, `linux-arm64`              | `rpm`, `deb`, `tar`
  | `osx-x64`, `osx-arm64`                  | `tar`
- **Publish libraries and/or applications to NuGet**
- **Upload all the package artifacts and your changelog to GitHub** on the tag associated with your package version (e.g your package is `1.0.0`, it will try to find a git tag `v1.0.0` or `1.0.0`).
- **Create a Homebrew repository and formula**  (e.g `user_or_org/homebrew-your-app-name`) for all the tar files associated with the targets for Linux and MacOS.

> Any of these steps can be configured or even entirely disabled easily from a config file.
> See the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md) on how to setup this differently for your application.
## Getting Started

- Install `dotnet-releaser` as a global .NET tool.
  ```
  dotnet tool install --global dotnet-releaser
  ```
- Go to a folder where you have your solution `.sln` file or your project file (`.csproj`, `.fsproj`, `.vbproj`) and run:
  ```
  dotnet releaser new
  ```
- It should create a `dotnet-releaser.toml` at the same level than your solution with a content like:
  ```toml
  [msbuild]
  project = "Tonlyn.sln"
  [github]
  user = "xoofx"
  repo = "Tomlyn"
  ```
- If you want to try a full build locally:
  ```
  dotnet-releaser build --force dotnet-releaser.toml
  ```
- If you want to integrate it to GitHub Action, use the `dotnet-releaser run` command. More details in the doc _[Adding dotnet-releaser to your CI on GitHub](https://github.com/xoofx/dotnet-releaser/tree/main/doc#12-adding-dotnet-releaser-to-your-ci-on-github)_. It is no more complicated than adding the following lines in your GitHub workflow file:
  ```yaml
    steps:
    - name: Checkout
      uses: actions/checkout@v4
      with:
        fetch-depth: 0

    - name: Install .NET 9.0
      uses: actions/setup-dotnet@v4
      with:
        dotnet-version: '9.0.x'

    - name: Build, Tests, Cover, Pack and Publish (on push tag)
      shell: bash
      run: |
        dotnet tool install --global dotnet-releaser
        dotnet-releaser run --nuget-token "${{secrets.NUGET_TOKEN}}" --github-token "${{secrets.GITHUB_TOKEN}}" src/dotnet-releaser.toml
  ```
  Notice the recommended usage of `shell: bash` so that if a secrets token is empty, bash won't remove the quotes, [unlike pwsh](https://github.com/PowerShell/PowerShell/issues/1995).

See the user guide below for further details on how to use `dotnet-releaser`.

## User Guide

For more details on how to use `dotnet-releaser`, please visit the [user guide](https://github.com/xoofx/dotnet-releaser/blob/main/doc/readme.md).
## License

This software is released under the [BSD-Clause 2 license](https://opensource.org/licenses/BSD-2-Clause). 

## Who is using `dotnet-releaser`?

It's brand new, so it's mainly used by the author for now! :innocent:

You can visit the `.github/workflows` folder, or check the release notes of the following projects to see `dotnet-releaser` in action:

Applications:
- [grpc-curl](https://github.com/xoofx/grpc-curl): An application shipping multiple executables.
- [lunet](https://github.com/lunet-io/lunet): An application shipping a .NET global tool to NuGet.
  
Regular .NET Libraries:
- [Markdig](https://github.com/xoofx/markdig)
- [Scriban](https://github.com/scriban/scriban)
- [Tomlyn](https://github.com/xoofx/Tomlyn)
- [Zio](https://github.com/xoofx/zio)
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

Alexandre Mutel aka [xoofx](https://xoofx.github.io).
