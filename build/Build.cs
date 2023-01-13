using System.Collections.Generic;
using Nuke.Common;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Tools.GitVersion;
using Nuke.Common.Tools.MSBuild;
using Nuke.Common.Tools.NuGet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.IO.FileSystemTasks;

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

    [GitVersion] readonly GitVersion GitVersion;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";

    Target CleanCompilationFolders => _ => _
        .After(LatestVersionInformation)
        .Executes(() => SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory));

    Target Compile => _ => _
        .DependsOn(CleanCompilationFolders)
        .DependsOn(LatestVersionInformation)
        .Executes(() =>
        {
            MSBuildTasks.MSBuild(s => s
                .SetTargetPath(Solution.SlideToAct_Binding)
                .SetConfiguration(Configuration)
                .SetAssemblyVersion(GitVersion.AssemblySemVer)
                .SetFileVersion(GitVersion.AssemblySemFileVer)
                .SetInformationalVersion(GitVersion.InformationalVersion)
                .SetTargets("Build"));
        });

    Target CleanArtifactsFolder => _ => _
        .After(Compile)
        .Executes(() => EnsureCleanDirectory(ArtifactsDirectory));

    Target Pack => _ => _
        .DependsOn(CleanArtifactsFolder)
        .DependsOn(Compile)
        .Executes(() =>
        {
            NuGetTasks.NuGetPack(s => s
                .DisableBuild()
                .SetTargetPath(Solution.SlideToAct_Binding)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .SetVersion(GitVersion.NuGetVersion));
        });

    [Secret] [Parameter] readonly string MyGetFeedUrl;
    [Secret] [Parameter] readonly string MyGetApiKey;

    Target PublishPackage => _ => _
        .DependsOn(Pack)
        .Requires(() => !string.IsNullOrEmpty(MyGetFeedUrl) && !string.IsNullOrEmpty(MyGetApiKey))
        .Executes(() =>
        {
            IEnumerable<AbsolutePath> artifactPackages = ArtifactsDirectory.GlobFiles("*.nupkg");

            DotNetTasks.DotNetNuGetPush(s => s
                .SetSource(MyGetFeedUrl)
                .SetApiKey(MyGetApiKey)
                .EnableSkipDuplicate()
                .CombineWith(artifactPackages, (_, v) => _
                    .SetTargetPath(v)));
        });
}