using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using DotNetReleaser.Configuration;
using DotNetReleaser.Logging;
using Scriban;
using Tomlyn;

namespace DotNetReleaser.Changelog;

public record ChangelogResult(string Title, string Body)
{
    public override string ToString()
    {
        return $"{Title}\n\n{Body}";
    }
}


public sealed class ChangelogTemplatizer
{
    private readonly ISimpleLogger _log;
    private readonly ChangelogConfiguration _config;
    private readonly Dictionary<ChangelogCategory, ChangelogCollection> _categoryToChanges;
    private Template _nameTemplateScriban;
    private Template _ownersCommitChangeTemplateScriban;
    private Template _ownersPullRequestChangeTemplateScriban;
    private Template _commitChangeTemplateScriban;
    private Template _pullRequestChangeTemplateScriban;
    private Template _bodyTemplateScriban;

    public ChangelogTemplatizer(ChangelogConfiguration config, ISimpleLogger log)
    {
        _log = log;
        _config = config;
        _categoryToChanges = new Dictionary<ChangelogCategory, ChangelogCollection>();

        _nameTemplateScriban = CompileScribanTemplate(_config.NameTemplate);
        _ownersCommitChangeTemplateScriban = CompileScribanTemplate(_config.OwnersCommitChangeTemplate);
        _ownersPullRequestChangeTemplateScriban = CompileScribanTemplate(_config.OwnersPullRequestChangeTemplate);
        _commitChangeTemplateScriban = CompileScribanTemplate(_config.CommitChangeTemplate); 
        _pullRequestChangeTemplateScriban = CompileScribanTemplate(_config.PullRequestChangeTemplate);
        _bodyTemplateScriban = CompileScribanTemplate(_config.BodyTemplate);
    }

    public ChangelogResult? Generate(ChangelogCollection changelogCollection)
    {
        DispatchPullRequestAndCommitChangesToCategories(changelogCollection);

        if (_log.HasErrors) return null;

        var changes = GenerateChanges();
        if (changes is null) return null;

        var title = SafeRender(_nameTemplateScriban, new { version = changelogCollection.Version }, "name");
        if (title is null) return null;

        // Create diff link
        var previousVersion = changelogCollection.PreviousVersion;
        var currentVersion = changelogCollection.Version;
        var compareLink = $"[{(previousVersion.HasVersion ? previousVersion.Tag : previousVersion.Sha)}...{(currentVersion.HasVersion ? currentVersion.Tag : currentVersion.Sha)}]({changelogCollection.CompareUrl})";
        
        var body = SafeRender(_bodyTemplateScriban, new { version = changelogCollection.Version, previous_version = changelogCollection.PreviousVersion, changes = changes, url_full_changelog_compare_changes = compareLink }, "body");
        if (body is null) return null;

        return new ChangelogResult(title, body);
    }

    private bool IsFromOwner(ChangelogChangeModel change) => _config.Owners.Any(x => EqualsIgnoreCase(change.Author, x));

    private string? SafeRender(Template template, object model, string context)
    {
        try
        {
            return template.Render(model);
        }
        catch (Exception ex)
        {
            _log.Error($"Error while rendering template {context}. Reason: {ex.Message}.");
            return null;
        }
    }
    
    private string? GenerateChanges()
    {
        var builder = new StringBuilder();
        foreach (var category in _config.Categories)
        {
            if (_categoryToChanges.TryGetValue(category, out var changesListPerCategory))
            {
                builder.AppendLine(category.Title);
                builder.AppendLine();

                foreach (var pr in changesListPerCategory.PullRequestChanges)
                {
                    var template = IsFromOwner(pr) ? _ownersPullRequestChangeTemplateScriban : _pullRequestChangeTemplateScriban;
                    var result = SafeRender(template, new { pr }, $"for category `{category.Title}`");
                    if (result is null) return null;
                    builder.AppendLine(result);
                }
                foreach (var commit in changesListPerCategory.CommitChanges)
                {
                    var template = IsFromOwner(commit) ? _ownersCommitChangeTemplateScriban : _commitChangeTemplateScriban;
                    var result = SafeRender(template, new { commit }, $"for category `{category.Title}`");
                    if (result is null) return null;
                    builder.AppendLine(result);
                }
                builder.AppendLine();
            }
        }

        return builder.ToString().Trim();
    }


    private static bool EqualsIgnoreCase(string left, string right)
    {
        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }


    private ChangelogCategory? FindCategoryFromLabel(string label)
    {
        foreach (var category in _config.Categories)
        {
            if (category.Exclude.Labels.Any(x => EqualsIgnoreCase(label, x)))
            {
                break;
            }

            if (category.Labels.Any(x => EqualsIgnoreCase(label, x)))
            {
                return category;
            }
        }

        return null;
    }

    private void DispatchPullRequestAndCommitChangesToCategories(ChangelogCollection changelogCollection)
    {
        var labelers = _config.Autolabeler.Select(x => x.Compile(_log)).ToList();

        // Exit if we had any errors when parsing auto-labelers
        if (_log.HasErrors) return;

        // Verify autolabelers
        foreach (var autolabeler in labelers)
        {
            var category = FindCategoryFromLabel(autolabeler.Label);
            if (category is null)
            {
                _log.Warn($"The autolabeler label `{autolabeler.Label}` is not used by a declared category.");
            }
        }

        // Dispatch Pull requests
        foreach (var pr in changelogCollection.PullRequestChanges)
        {
            bool exclude = false;
            bool added = false;

            // If we have an include labels list and the PR doesn't have it, don't include the PR change
            if (_config.IncludeLabels.Count > 0 && !_config.IncludeLabels.Any(x => pr.Labels.Any(y => EqualsIgnoreCase(x, y)))
                // Check if the PR is excluded by labels
                || _config.Exclude.Labels.Any(x => pr.Labels.Any(y => EqualsIgnoreCase(x, y)))
                // Check if the PR is excluded by contributors
                || _config.Exclude.Contributors.Any(x => EqualsIgnoreCase(pr.Author, x))
                )
            {
                continue;
            }

            // Look at categories
            foreach (var category in _config.Categories)
            {
                if (category.Exclude.Labels.Any(x => pr.Labels.Any(y => EqualsIgnoreCase(x, y)))
                    || category.Exclude.Contributors.Any(x => EqualsIgnoreCase(pr.Author, x)))
                {
                    exclude = true;
                    break;
                }

                if (category.Labels.Any(x => pr.Labels.Any(y => EqualsIgnoreCase(x, y))))
                {
                    AddPrToCategory(pr, category);
                    added = true;
                    break;
                }
            }

            // If not added and not excluded, then try auto-labelers
            if (!added && !exclude)
            {
                foreach (var autolabeler in labelers)
                {
                    if (autolabeler.Match(pr))
                    {
                        var category = FindCategoryFromLabel(autolabeler.Label);
                        if (category is not null)
                        {
                            AddPrToCategory(pr, category);
                        }
                        break;
                    }
                }
            }
        }

        // Dispatch commits
        if (_config.IncludeCommits)
        {
            foreach (var commit in changelogCollection.CommitChanges)
            {
                foreach (var autolabeler in labelers)
                {
                    if (autolabeler.Match(commit))
                    {
                        var category = FindCategoryFromLabel(autolabeler.Label);
                        if (category is not null)
                        {
                            if (!_categoryToChanges.TryGetValue(category, out var subCollection))
                            {
                                subCollection = new ChangelogCollection();
                                _categoryToChanges[category] = subCollection;
                            }

                            subCollection.CommitChanges.Add(commit);
                            break;
                        }
                    }
                }
            }
        }

        void AddPrToCategory(ChangelogPullRequestChangeModel pr, ChangelogCategory category)
        {
            if (!_categoryToChanges.TryGetValue(category, out var subCollection))
            {
                subCollection = new ChangelogCollection();
                _categoryToChanges[category] = subCollection;
            }
            subCollection.PullRequestChanges.Add(pr);
        }
    }

    private Template CompileScribanTemplate(string text, [CallerArgumentExpression("text")] string? textExpression = null)
    {
        var template = Template.Parse(text);

        if (template.HasErrors)
        {
            string context = "unknown";
            if (textExpression?.IndexOf('.') >= 0)
            {
                context = textExpression.Substring(textExpression.IndexOf('.') + 1);
                var propertyInfo = typeof(ChangelogConfiguration).GetProperty(context);
                if (propertyInfo != null)
                {
                    var tomlModelOptions = new TomlModelOptions();
                    context = tomlModelOptions.GetPropertyName(propertyInfo) ?? "unknown";
                }
            }

            _log.Error($"The template property `changelog.{context}` has template errors:\n{template.Messages}");
        }

        return template;
    }
}