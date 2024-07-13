using System.Collections.Generic;
using System.Linq;
using DotNetReleaser.Logging;
using LibGit2Sharp;

namespace DotNetReleaser;

public class GitInformation
{
    private GitInformation(Repository repository, Branch branch, string branchName)
    {
        Repository = repository;
        Head = repository.Head.Tip;
        Branch = branch;
        BranchName = branchName;
    }

    public Repository Repository { get; }

    public Branch Branch { get; }

    public string BranchName { get; }

    public Commit Head { get; }

    public static GitInformation? Create(ISimpleLogger logger, string searchPath, List<string> branches)
    {
        var repositoryPath = Repository.Discover(searchPath);

        if (repositoryPath is null)
        {
            return null;
        }

        var repository = new Repository(repositoryPath);

        var branchesFound = repository.Branches.Where(branch => branch.Tip != null && branch.Tip.Sha == repository.Head.Tip.Sha).Select(x => (Branch: x, BranchName: GetShortBranchName(x))).ToList();
        var branchNamesFound = branchesFound.Select(x => x.BranchName).ToList();

        var branchName = branchNamesFound.FirstOrDefault(branches.Contains) ?? branchNamesFound.FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrEmpty(branchName))
        {
            logger.Error($@"Unable to retrieve the current branch from the commit {repository.Head.Tip.Sha}. The current action requires it. Please make sure that:
1) The current commit is a checkout on a valid branch.
2) If running on GitHub Action, you are using `actions/checkout@v4` and not v1, but also that the property `fetch-depth: 0` is correctly setup.
3) We have found the following branches containing this commit [{string.Join(",", repository.Branches.Select(x => x.FriendlyName))}]. 
4) You pushed only tags (`git push --tags`) and forget to push whole branch.h
");
            return null;
        }

        var branchAndName = branchesFound.First(x => x.BranchName == branchName);

        return new GitInformation(repository, branchAndName.Branch, branchAndName.BranchName);
    }

    private static string GetShortBranchName(Branch branch)
    {
        var branchName = branch.FriendlyName;

        // If we have a remote branch, extract the local name
        if (branch.IsRemote)
        {
            var branchNameParts = branch.FriendlyName.Split('/');
            if (branchNameParts.Length == 2)
            {
                branchName = branchNameParts[1];
            }
        }

        return branchName;
    }
}
