﻿using Nuke.Common.Tools.DotNet;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

partial class Build
{
    Target Pack => _ => _
        .TriggeredBy(Compile)
        .Executes(() =>
        {
            foreach (var configuration in GlobBuildConfigurations())
                DotNetPack(settings => settings
                    .SetConfiguration(configuration)
                    .SetVersion(VersionMap[configuration])
                    .SetOutputDirectory(ArtifactsDirectory)
                    .SetVerbosity(DotNetVerbosity.Minimal));
        });
}