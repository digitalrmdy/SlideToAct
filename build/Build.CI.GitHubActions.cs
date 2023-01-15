using Nuke.Common.CI.GitHubActions;

[GitHubActions(
    "PR",
    GitHubActionsImage.MacOs12,
    AutoGenerate = true,
    EnableGitHubToken = true,
    FetchDepth = 0,
    OnPullRequestBranches = new[] { "main" },
    InvokedTargets = new[] { nameof(Compile) },
    CacheKeyFiles = new string[0],
    CacheIncludePatterns = new string[0])]
[GitHubActions(
    "Publish",
    GitHubActionsImage.MacOs12,
    AutoGenerate = true,
    EnableGitHubToken = true,
    FetchDepth = 0,
    OnPushTags = new[] { "'*.*.*'" },
    InvokedTargets = new[] { nameof(PublishPackage) },
    ImportSecrets = new[] { nameof(PackageFeedUrl), nameof(PackageFeedApiKey) },
    CacheKeyFiles = new string[0],
    CacheIncludePatterns = new string[0])]
partial class Build
{
}