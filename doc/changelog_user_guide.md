# Changelog User Guide

`dotnet-releaser` allows to generate changelogs that will be pushed as release notes. It **supports 2 modes**:

- **Automatic**

  This is the default mode that will generate automatically pretty changelogs directly from your Pull Requests and Commits changes, fully customized with categories, auto-labelers, contributors/labels filtering and templates. If you are familiar with solutions like [Release Drafter](https://github.com/release-drafter/release-drafter), `dotnet-releaser` provides similar integrated features and goes beyond.

- **Manual**

  This mode is more interesting if you are writing your changelog manually in a file and want to transfer this automatically to your release notes.

Table of Content

- [1. Automatic changelogs](#1-automatic-changelogs)
- [2. Manual changelogs](#2-manual-changelogs)
- [3. Disabling](#3-disabling)
  

## 1. Automatic changelogs




## 2. Manual changelogs

`dotnet-releaser` can help to transfer your manually written changelog (e.g from a `changelog.md`) to your GitHub release for a specific version of the package published.

| `[changelog]`    | Type       | Description                |
|------------------|------------|----------------------------|
| `publish`        | `bool`     | Enable to disable changelog publish. Default is `true`.
| `path`           | `string`   | Path to a changelog file. The path can be relative to the configuration file.
| `version`        | `string`   | A regular expression used to identify the changelogs for a specific version. The default regex `^#+\s+v?((\d+\.)*(\d+))` will match any classic markdown headers that contain a version number, optionally pre-fixed by `v`.

For example, if your changelog is written like this in one folder above the configuration file:

```md

# Changelog

## 1.3.1 (27 Oct 2021)

### Fixes
- Fix for this annoying bug...

### Breaking changes
- ...
```

And your configuration for the changelog can be described like this:

```toml
[changelog]
path = "../changelog.md" # Setting the path to a manual changelog.

If you are publishing the `1.3.1` version of your package, it will extract the markdown after the `## 1.3.1` header:

```md
### Fixes
- Fix for this annoying bug...

### Breaking changes
- ...
```

And this will be uploaded to your tag release.


## 3. Disabling

**Changelog is enabled by default** but can be disabled entirely by setting `publish = false`:

```toml
[changelog]
publish = false
```