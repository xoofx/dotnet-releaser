namespace DotNetReleaser;

public partial class ReleaserApp
{
    private void UpdateHomebrewConfigurationFromPackage(PackageInfo packageInfo)
    {
        if (!_config.Brew.Publish) return;

        _config.Brew.Home = string.IsNullOrEmpty(_config.Brew.Home) ? $"homebrew-{packageInfo.ExeName}" : _config.Brew.Home;

        Info($"The configured homebrew destination repository is `{_config.GitHub.User}/{_config.Brew.Home}`.");
    }
}