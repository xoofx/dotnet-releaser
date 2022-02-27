using System.Collections.Generic;
using System.IO;
using System.Linq;
using DotNetReleaser.Logging;
using LibGit2Sharp;

namespace DotNetReleaser;

public class GitInformation
{
    private GitInformation(Repository repository, Branch branch)
    {
        Repository = repository;
        Head = repository.Head.Tip;
        Branch = branch;
    }

    public Repository Repository { get; }

    public Branch Branch { get; }

    public string BranchName => Branch.FriendlyName;

    public Commit Head { get; }

    public static GitInformation? Create(ISimpleLogger logger, string searchPath, List<string> branches)
    {
        var repositoryPath = Repository.Discover(searchPath);

        if (repositoryPath is null)
        {
            return null;
        }

        var repository = new Repository(repositoryPath);

        var branchesFound = repository.Branches.Where(branch => branch.Tip != null && branch.Tip.Sha == repository.Head.Tip.Sha).ToList();
        var branchNamesFound = branchesFound.Select(x => x.FriendlyName).ToList();

        var branchName = branchNamesFound.FirstOrDefault(branches.Contains) ?? branchNamesFound.FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(branchName))
        {
            logger.Error($@"Unable to retrieve the current branch from the commit {repository.Head.Tip.Sha}. The current action requires it. Please make sure that:
1) The current commit is a checkout on a valid branch.
2) If running on GitHub Action, you are using `actions/checkout@v2` and not v1, but also that the property `fetch-depth: 0` is correctly setup.
3) We have found the following branches containing this commit [{string.Join(",", repository.Branches.Select(x => x.FriendlyName))}]. 
");
            return null;
        }

        var branch = branchesFound.First(x => x.FriendlyName == branchName);

        return new GitInformation(repository, branch);
    }
}