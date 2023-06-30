using Nuke.Common;
using Nuke.Common.Tooling;
using Serilog;

partial class Build
{
    [LatestGitHubRelease(
        identifier: "cortinico/slidetoact",
        IncludePrerelease = false,
        TrimPrefix = true)]
    readonly string LatestGitHubReleaseVersion;

    [LatestMavenVersion(
        repository: "repo.maven.apache.org/maven2",
        groupId: "com.ncorti",
        artifactId: "slidetoact")]
    readonly string LatestMavenVersion;

    Target LatestVersionInformation => _ => _
        .ProceedAfterFailure()
        .Executes(() =>
        {
            Log.Information("Latest SlideToAct GitHub release version: {LatestGitHubReleaseVersion}",
                LatestGitHubReleaseVersion);
            Log.Information("Latest SlideToAct Maven version: {LatestMavenVersion}", LatestMavenVersion);
        });
}