using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Nuke.Common;
using Nuke.Common.Git;
using Serilog;
using Serilog.Events;

partial class Build
{
    Target CreateInstaller => _ => _
        .TriggeredBy(Compile)
<!--#if (!NoPipeline)
        .OnlyWhenStatic(() => IsLocalBuild || GitRepository.IsOnMainOrMasterBranch())
#endif-->
        .Executes(() =>
        {
            foreach (var (installer, project) in InstallersMap)
            {
                Log.Information("Project: {Name}", project.Name);

                var exePattern = $"*{installer.Name}.exe";
                var exeFile = Directory.EnumerateFiles(installer.Directory, exePattern, SearchOption.AllDirectories).First();

                var publishDirectories = Directory.GetDirectories(project.Directory, "Publish*", SearchOption.AllDirectories);
                if (publishDirectories.Length == 0) throw new Exception("No files were found to create an installer");

                var proc = new Process();
                proc.StartInfo.FileName = exeFile;
                proc.StartInfo.Arguments = BuildExeArguments(publishDirectories);
                proc.StartInfo.RedirectStandardOutput = true;
                proc.StartInfo.RedirectStandardError = true;
                proc.Start();

                RedirectStream(proc.StandardOutput, LogEventLevel.Information);
                RedirectStream(proc.StandardError, LogEventLevel.Error);

                proc.WaitForExit();
                if (proc.ExitCode != 0) throw new Exception($"The installer creation failed with ExitCode {proc.ExitCode}");
            }
        });

    [SuppressMessage("ReSharper", "TemplateIsNotCompileTimeConstantProblem")]
    void RedirectStream(StreamReader reader, LogEventLevel eventLevel)
    {
        while (!reader.EndOfStream)
        {
            var value = reader.ReadLine();
            if (value is null) continue;

            var matches = ArgumentsRegex.Matches(value);
            if (matches.Count > 0)
            {
                var parameters = matches
                    .Select(match => match.Value.Substring(1, match.Value.Length - 2))
                    .Cast<object>()
                    .ToArray();

                var line = ArgumentsRegex.Replace(value, match => $"{{Parameter{match.Index}}}");
                Log.Write(eventLevel, line, parameters);
            }
            else
            {
                Log.Debug(value);
            }
        }
    }

    static string BuildExeArguments(IReadOnlyList<string> args)
    {
        var argumentBuilder = new StringBuilder();
        for (var i = 0; i < args.Count; i++)
        {
            if (i > 0) argumentBuilder.Append(' ');
            var value = args[i];
            if (value.Contains(' ')) value = $"\"{value}\"";
            argumentBuilder.Append(value);
        }

        return argumentBuilder.ToString();
    }
}