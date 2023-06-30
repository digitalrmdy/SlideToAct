using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode
    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution(GenerateProjects = true)] readonly Solution Solution;
    
    [GitRepository] readonly GitRepository GitRepository;

    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target Clean => _ => _
        .Before(Restore)
        .After(LatestVersionInformation)
        .Executes(() =>
        {
            SourceDirectory
                .GlobDirectories("**/bin", "**/obj")
                .ForEach(ap => ap.DeleteDirectory());
            ArtifactsDirectory.CreateOrCleanDirectory();
        });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(s => s
                .SetProjectFile(Solution)
                .EnableDeterministic()
                .EnableContinuousIntegrationBuild());
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .DependsOn(LatestVersionInformation)
        .Executes(() =>
        {
            DotNetBuild(s => s
                .EnableNoRestore()
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetDeterministic(IsServerBuild)
                .SetContinuousIntegrationBuild(IsServerBuild)
                .SetVersion(GitVersion.FullSemVer)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion));
        });

    Target Pack => _ => _
        .DependsOn(Clean, Compile)
        .Executes(() =>
        {
            DotNetPack(s => s
                .EnableNoRestore()
                .EnableNoBuild()
                .SetProject(Solution)
                .SetConfiguration(Configuration)
                .SetVersion(GitVersion.NuGetVersion)
                .SetProperty("RepositoryBranch", GitRepository.Branch)
                .SetProperty("RepositoryCommit", GitRepository.Commit)
                .SetOutputDirectory(ArtifactsDirectory));
        });

    [Secret] [Parameter] readonly string PackageFeedUrl;
    [Secret] [Parameter] readonly string PackageFeedApiKey;

    Target PublishPackage => _ => _
        .DependsOn(Pack)
        .Requires(() => !string.IsNullOrEmpty(PackageFeedUrl) && !string.IsNullOrEmpty(PackageFeedApiKey))
        .Executes(() =>
        {
            IEnumerable<AbsolutePath> artifactPackages = ArtifactsDirectory.GlobFiles("*.nupkg");

            DotNetNuGetPush(s => s
                .SetSource(PackageFeedUrl)
                .SetApiKey(PackageFeedApiKey)
                .EnableSkipDuplicate()
                .CombineWith(artifactPackages, (_, v) => _
                    .SetTargetPath(v)));
        });
}