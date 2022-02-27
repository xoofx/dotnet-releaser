using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetReleaser.Coverage.Coveralls;
using DotNetReleaser.Helpers;

namespace DotNetReleaser;

public partial class ReleaserApp
{
    private async Task PublishCoveralls(IDevHosting devHosting, BuildInformation buildInfo)
    {
        if (!_config.Coveralls.Publish || _assemblyCoverages.Count == 0) return;

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
                    CommitterEmail = gitInfo.Head.Committer.Name,
                    CommitterName = gitInfo.Head.Committer.Email,
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