﻿using AbpDevTools.Configuration;
using CliFx.Infrastructure;
using Spectre.Console;

namespace AbpDevTools.Commands;

[Command("logs")]
public class LogsCommand : ICommand
{
    [CommandParameter(0, Description = "Determines the project to open logs of it.", IsRequired = false)]
    public string? ProjectName { get; set; }

    [CommandOption("path", 'p', Description = "Working directory of the command. Probably solution directory. Default: . (CurrentDirectory) ")]
    public string? WorkingDirectory { get; set; }

    [CommandOption("interactive", 'i', Description = "Options will be asked as prompt when this option used.")]
    public bool Interactive { get; set; }

    protected readonly RunConfiguration runConfiguration;
    protected readonly Platform platform;

    public LogsCommand(RunConfiguration runConfiguration, Platform platform)
    {
        this.runConfiguration = runConfiguration;
        this.platform = platform;
    }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        if (string.IsNullOrEmpty(WorkingDirectory))
        {
            WorkingDirectory = Directory.GetCurrentDirectory();
        }

        var _runnableProjects = runConfiguration.GetOptions().RunnableProjects;
        var csprojs = await AnsiConsole.Status()
            .StartAsync("Looking for projects...", async ctx =>
            {
                ctx.Spinner(Spinner.Known.SimpleDotsScrolling);

                await Task.Yield();

                var projects = Directory.EnumerateFiles(WorkingDirectory, "*.csproj", SearchOption.AllDirectories)
                    .Where(x => _runnableProjects.Any(y => x.EndsWith(y + ".csproj")))
                    .Select(x => new FileInfo(x))
                    .ToArray();
                AnsiConsole.MarkupLine($"[green]{projects.Length}[/] .sln files found.");

                return projects;
            });

        if (string.IsNullOrEmpty(ProjectName))
        {
            if (Interactive)
            {
                await console.Output.WriteLineAsync($"\n");
                ProjectName = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                        .Title("Choose a [mediumpurple2]project[/] to open logs?")
                        .PageSize(12)
                        .HighlightStyle(new Style(foreground: Color.MediumPurple2))
                        .MoreChoicesText("[grey](Move up and down to reveal more rules)[/]")
                        .AddChoices(csprojs.Select(s => s.Name)));
            }
            else
            {
                await console.Output.WriteLineAsync("You have to pass a project name.\n");
                await console.Output.WriteLineAsync("\n\tUsage:");
                await console.Output.WriteLineAsync("\tlogs -p <project-name>");
                await console.Output.WriteLineAsync("\nAvailable project names:\n\n\t - " +
                    string.Join("\n\t - ", csprojs.Select(x => x.Name.Split(Path.DirectorySeparatorChar).Last())));
                return;
            }
        }

        var selectedCsproj = csprojs.FirstOrDefault(x => x.FullName.Contains(ProjectName));

        if (selectedCsproj == null)
        {
            await console.Output.WriteLineAsync($"No project found with the name '{ProjectName}'");
            return;
        }

        var dir = Path.GetDirectoryName(selectedCsproj.FullName)!;
        var logsDir = Path.Combine(dir, "Logs");
        if (Directory.Exists(logsDir))
        {
            var filePath = Path.Combine(logsDir, "logs.txt");
            if (File.Exists(filePath))
            {
                platform.Open(filePath);
            }
            else
            {
                platform.Open(logsDir);
            }
        }
        else
        {
            await console.Output.WriteLineAsync("No logs folder found for project.\nOpening project folder...");

            platform.Open(dir);
        }
    }
}
