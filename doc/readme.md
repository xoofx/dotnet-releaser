# `dotnet-releaser` User Guide

- [0) General usage](#0-general-usage)
- [1) Commands](#1-commands)
  - [1.1) `dotnet-releaser new`](#11-dotnet-releaser-new)
  - [1.2) `dotnet-releaser build`](#12-dotnet-releaser-build)
  - [1.3) `dotnet-releaser publish`](#13-dotnet-releaser-publish)
- [2) Configuration](#2-configuration)
  - [2.1) General](#21-general)
  - [2.2) MSBuild](#22-msbuild)
  - [2.3) GitHub](#23-github)
  - [2.4) Packaging](#24-packaging)
  - [2.5) NuGet](#25-nuget)
  - [2.6) Homebrew](#26-homebrew)
  - [2.7) Changelog](#27-changelog)

## 0) General usage

Get some help by typing `dotnet-releaser --help`

```
dotnet-releaser 0.1.0 - 2022 (c) Copyright Alexandre Mutel

Usage: dotnet-releaser [command] [options]

Options:
  --version     Show version information.
  -?|-h|--help  Show help information.

Commands:
  build         Build only the project.
  new           Create a dotnet-releaser TOML configuration file for a specified project.
  publish       Build and publish the project.

Run 'dotnet-releaser [command] -?|-h|--help' for more information about a command.
```
## 1) Commands

### 1.1) `dotnet-releaser new`

```
Create a dotnet-releaser TOML configuration file for a specified project.

Usage: dotnet-releaser new [options] <dotnet-releaser.toml>

Arguments:
  dotnet-releaser.toml      TOML configuration file path to create. Default is: dotnet-releaser.toml

Options:
  --project <project_file>  A - relative - path to project file (csproj, vbproj, fsproj)
  --user <GitHub_user/org>  The GitHub user/org where the packages will be published
  --repo <GitHub_repo>      The GitHub repo name where the packages will be published
  --force                   Force overwriting the existing TOML configuration file.
  -?|-h|--help              Show help information.
```

Example:

```shell
$ dotnet-releaser new --project HelloWorld.csproj --user xoofx --repo HelloWorld
``` 

The command above will create a `dotnet-releaser.toml` configuration file. See [](#)


### 1.2) `dotnet-releaser build`

```
Build only the project.

Usage: dotnet-releaser build [options] <dotnet-releaser.toml>

Arguments:
  dotnet-releaser.toml  TOML configuration file

Options:
  --force               Force deleting and recreating the artifacts folder.
  -?|-h|--help          Show help information.
```

Example:

```shell
$ dotnet-releaser build --project HelloWorld.csproj --user xoofx --repo HelloWorld
``` 

### 1.3) `dotnet-releaser publish`

```
Build and publish the project.

Usage: dotnet-releaser publish [options] <dotnet-releaser.toml>

Arguments:
  dotnet-releaser.toml    TOML configuration file

Options:
  --github-token <token>  GitHub Api Token. Required if publish to GitHub is true in the config file
  --nuget-token <token>   NuGet Api Token. Required if publish to NuGet is true in the config file
  --force                 Force deleting and recreating the artifacts folder.
  -?|-h|--help            Show help information.
```

## 2) Configuration

The configuration is all done with a configuration file in the [TOML](https://toml.io/en/) format.

### 2.1) General

The following properties can only be set before any of the sections (e.g `[msbuild]`, `[nuget]`...)

> `profile`

Defines which packs are created by default. See [packaging](#24-packaging) for more details.

```toml
# This is the default, creating all the OS/CPU/Packages listed on the front readme.md
profile = "default"
# This will make no default packs. You need to configure them manually
profile = "custom"
```
___

> `artifacts_folder`

Defines to which folder to output created packages. By default it is setup to `artifacts-dotnet-releaser` relative to where to TOML configuration file is.

```toml
# This is the default, creating all the OS/CPU/Packages listed on the front readme.md
artifacts_folder = "myfolder"
```
___

### 2.2) MSBuild

This section defines:

- The application project to build. This **property is mandatory**. There is no default!
- The MSBuild configuration (e.g `Debug` or `Release`). Default is `Release`
- Additional MSBuild properties

Example:

```toml
# MSBuild section
[msbuild]
project = "../Path/To/My/Project.csproj"
configuration = "Release"
[msbuild.properties]
PublishReadyToRun = false # Disable PublishReadyToRun
```
___

> `msbuild.project`

Specifies the path to the project to compile with MSBuild. If this path uses a relative path, it will be relative to the location of your TOML configuration file.

___

> `msbuild.configuration`

Specifies the MSBuild `Configuration` property. By default this is set to `Release`.

___

> `[msbuild.properties]`

By default, `dotnet-releaser` is using the following MSBuild defaults for configuring your application as a single file/self contained application:

```toml
# Default values used by `dotnet-releaser`
[msbuild.properties]
PublishTrimmed = true
PublishSingleFile = true
SelfContained = true
PublishReadyToRun = true 
CopyOutputSymbolsToPublishDirectory = false
SkipCopyingSymbolsToOutputDirectory = true 
``` 

But you can completely override these property values.
___

### 2.3) GitHub



### 2.4) Packaging


### 2.5) NuGet

### 2.6) Homebrew

### 2.7) Changelog

















