using System.Linq;
using System.Reflection;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities;
using Nuke.Common.ValueInjection;
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
        .Executes(() =>
        {
            Log.Information("Latest SlideToAct GitHub release version: {LatestGitHubReleaseVersion}",
                LatestGitHubReleaseVersion);
            Log.Information("Latest SlideToAct Maven version: {LatestMavenVersion}", LatestMavenVersion);
        });

    /// <summary>
    /// Custom LatestMavenVersion attribute because of a bug regarding the url construction.
    /// This issue is also tracked (and triaged) by https://github.com/nuke-build/nuke/issues/1103
    /// </summary>
    /// <remark>
    /// The bug in question is a discrepancy between maven api implementations regarding a section of the url,
    /// "m2" vs "maven2".
    /// Eg:
    ///     https://plugins.gradle.org/m2/org/jetbrains/ ...
    ///     vs
    ///     https://repo.maven.apache.org/maven2/org/jetbrains/ ...
    /// </remark>
    class LatestMavenVersionAttribute : ValueInjectionAttributeBase
    {
        readonly string _repository;
        readonly string _groupId;
        readonly string _artifactId;

        public LatestMavenVersionAttribute(string repository, string groupId, string artifactId = null)
        {
            _repository = repository;
            _groupId = groupId;
            _artifactId = artifactId;
        }

        public override object GetValue(MemberInfo member, object instance)
        {
            var endpoint = _repository.TrimStart("https").TrimStart("http").TrimStart("://").TrimEnd("/");
            var uri = $"https://{endpoint}/{_groupId.Replace(".", "/")}/{_artifactId ?? _groupId}/maven-metadata.xml";
            var content = HttpTasks.HttpDownloadString(uri);
            return XmlTasks.XmlPeekFromString(content, ".//latest").Single();
        }
    }
}