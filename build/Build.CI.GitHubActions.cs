using Nuke.Common.CI.GitHubActions;

[GitHubActions(
    "PR",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    EnableGitHubToken = true,
    FetchDepth = 0,
    InvokedTargets = new[] { nameof(Compile) },
    OnPullRequestBranches = new[] { "main" },
    CacheKeyFiles = new string[0],
    CacheIncludePatterns = new string[0])]
[GitHubActions(
    "Publish",
    GitHubActionsImage.WindowsLatest,
    AutoGenerate = true,
    EnableGitHubToken = true,
    FetchDepth = 0,
    ImportSecrets = new[] { nameof(PackageFeedUrl), nameof(PackageFeedApiKey) },
    InvokedTargets = new[] { nameof(PublishPackage) },
    OnPushTags = new[] { "'*.*.*'" },
    CacheKeyFiles = new string[0],
    CacheIncludePatterns = new string[0])]
partial class Build
{
}