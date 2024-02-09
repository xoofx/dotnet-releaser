using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetReleaser.Coverage;
using DotNetReleaser.Coverage.Coveralls;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task PublishCoverageToGist(IDevHosting devHosting, BuildInformation buildInfo, HitCoverage coverage)
    {
        if (!_config.Coverage.BadgeUploadToGist || !buildInfo.IsPush) return;

        var gistId = _config.Coverage.BadgeGistId;
        if (string.IsNullOrWhiteSpace(gistId))
        {
            Warn("The 'coverage.badge_gist_id' is not set in the configuration file. The coverage badge will not be uploaded to a gist.");
            return;
        }
        
        var rate = (int)Math.Round((double)coverage.Rate * 100);

        // TODO: We could make many of these things configurable (colors, size of the badge, etc.)
        var color = rate switch
        {
            >= 95 => "#4c1",
            >= 90 => "#a3c51c",
            >= 75 => "#dfb317",
            _ => "#e05d44"
        };
        
        var svg = $"""
                  <svg xmlns="http://www.w3.org/2000/svg" width="99" height="20"><linearGradient id="a" x2="0" y2="100%"><stop offset="0" stop-color="#bbb" stop-opacity=".1"/><stop offset="1" stop-opacity=".1"/></linearGradient><rect rx="3" width="99" height="20" fill="#555"/><rect rx="3" x="63" width="36" height="20" fill="{color}"/><path fill="{color}" d="M63 0h4v20h-4z"/><rect rx="3" width="99" height="20" fill="url(#a)"/><g fill="#fff" text-anchor="middle" font-family="DejaVu Sans,Verdana,Geneva,sans-serif" font-size="11"><text x="32.5" y="15" fill="#010101" fill-opacity=".3">coverage</text><text x="32.5" y="14">coverage</text><text x="80" y="15" fill="#010101" fill-opacity=".3">{rate:##}%</text><text x="80" y="14">{rate:##}%</text></g></svg>
                  """;

        var fileName = $"dotnet-releaser-coverage-badge-{_config.GitHub.User}-{_config.GitHub.Repo}.svg";
        Info($"Updating coverage badge with {rate:##}% result to gist {gistId} and file {fileName}");
        await devHosting.CreateOrUpdateGist(gistId, fileName, svg);
    }

    private async Task PublishCoveralls(IDevHosting devHosting, BuildInformation buildInfo)
    {
        if (!_config.Coveralls.Publish || _assemblyCoverages.Count == 0 || !buildInfo.IsPush) return;

        var ownerRepo = $"{devHosting.Configuration.User}/{devHosting.Configuration.Repo}";

        var baseUri = new Uri(_config.Coveralls.Url);
        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(new Uri(baseUri, $"/github/{ownerRepo}.json"));
        if (!response.IsSuccessStatusCode)
        {
            return;
        }

        try
        {
            _logger.LogStartGroup($"Publishing Coverage Results to {_config.Coveralls.Url}");
            
            // Can't publish if we don't get a JSON result from last build
            if (response.Content.Headers.ContentType?.MediaType != "application/json")
            {
                Warn($"Can't publish to coveralls.io as the repository {ownerRepo} is not registered there.");
                return;
            }

            if (buildInfo.GitInformation is null)
            {
                Warn($"Can't publish to coveralls.io as there is no git configured.");
                return;
            }

            var gitInfo = buildInfo.GitInformation;
            string rootDirectory = Path.GetFullPath(gitInfo.Repository.Info.WorkingDirectory);
            var sourceFiles = CoverallsHelper.ConvertToCoverallsSourceFiles(_logger, _assemblyCoverages, rootDirectory);
            if (_logger.HasErrors) return;

            var result = new CoverallsData(devHosting.ApiToken)
            {
                CommitSha = gitInfo.Head.Sha
            };
            result.SourceFiles.AddRange(sourceFiles);

            var githubInfo = GitHubActionHelper.GetInfo();
            if (githubInfo is not null)
            {
                result.ServiceJobId = Environment.GetEnvironmentVariable("GITHUB_RUN_ID");
                if (githubInfo.EventName == "pull_request" || githubInfo.EventName == "pull_request_target")
                {
                    if (githubInfo.Event.TryGetValue("number", out var prNumberObj) && prNumberObj is int prNumber)
                    {
                        result.ServicePullRequest = prNumber.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }

            result.Git = new GitData()
            {
                Branch = gitInfo.BranchName,
                Head = new GitHeadData()
                {
                    Id = gitInfo.Head.Sha,
                    AuthorName = gitInfo.Head.Author.Name,
                    AuthorEmail = gitInfo.Head.Author.Email,
                    CommitterName = gitInfo.Head.Committer.Name,
                    CommitterEmail = gitInfo.Head.Committer.Email,
                    Message = gitInfo.Head.MessageShort,
                },
                Remotes =
                {
                    new GitRemoteData()
                    {
                        Name = "origin",
                        Url = $"{devHosting.Configuration.GetUrl()}.git",
                    }
                }
            };

            // Declare a new HttpClient with compression
            //httpClient = new HttpClient(new GzipCompressingHandler(new HttpClientHandler()
            //{
            //    AutomaticDecompression = DecompressionMethods.All
            //}));

            // TODO: could compress the output
            var json = result.ToJson();
            using var formData = new MultipartFormDataContent
            {
                { new StringContent(json), "json" }
            };

            // Credits fix from https://github.com/csMACnz/coveralls.net/issues/110#issuecomment-1203220933
            var boundary = formData.Headers.ContentType?.Parameters.FirstOrDefault(o => o.Name == "boundary");
            if (boundary != null)
            {
                boundary.Value = boundary.Value?.Replace("\"", string.Empty);
            }

            var postResponse = await httpClient.PostAsync(new Uri(baseUri, "/api/v1/jobs"), formData);

            if (!postResponse.IsSuccessStatusCode)
            {
                string errorMessage = "(empty)";
                if (postResponse.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    errorMessage = await postResponse.Content.ReadAsStringAsync();
                    if (errorMessage.StartsWith("{"))
                    {
                        try
                        {
                            var errorObject = JsonHelper.FromString(errorMessage) as Dictionary<string, object?>;
                            if (errorObject is not null)
                            {
                                if (errorObject.TryGetValue("message", out var messageObject) && messageObject is string text)
                                {
                                    errorMessage = text;
                                }
                            }
                        }
                        catch
                        {
                            // 
                        }
                    }
                }

                Error($"Error while publishing to {_config.Coveralls.Url}. Status: {postResponse.StatusCode}/{(int)postResponse.StatusCode}, Reason: {postResponse.ReasonPhrase}. Error Message: {errorMessage}");
            }
            else
            {
                Info($"Coverage results have been published successfully to {_config.Coveralls.Url}");
            }
        }
        finally
        {
            _logger.LogEndGroup();
        }
    }
}