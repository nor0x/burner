using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace Burner.Commands;

public class OpenCommandSettings : CommandSettings
{
	[CommandArgument(0, "<NAME>")]
	[Description("Project name to open")]
	public string Name { get; set; } = string.Empty;

	[CommandOption("-e|--explorer")]
	[Description("Open in file explorer instead of terminal")]
	public bool Explorer { get; set; }

	[CommandOption("-c|--code")]
	[Description("Open in VS Code")]
	public bool Code { get; set; }
}

public class OpenCommand : Command<OpenCommandSettings>
{
	public override int Execute(CommandContext context, OpenCommandSettings settings)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		var project = projectService.GetProject(settings.Name);
		if (project == null)
		{
			AnsiConsole.MarkupLine($"[red]Project not found:[/] {settings.Name}");

			// Suggest similar projects
			var allProjects = projectService.GetAllProjects().ToList();
			var similar = allProjects
				.Where(p => p.Name.Contains(settings.Name, StringComparison.OrdinalIgnoreCase))
				.Take(3)
				.ToList();

			if (similar.Any())
			{
				AnsiConsole.MarkupLine("[grey]Did you mean:[/]");
				foreach (var p in similar)
				{
					AnsiConsole.MarkupLine($"  [blue]{p.Name}[/]");
				}
			}

			return 1;
		}

		if (settings.Code)
		{
			OpenInVSCode(project.Path);
		}
		else if (settings.Explorer)
		{
			OpenInExplorer(project.Path);
		}
		else
		{
			// Default: print the path (useful for cd `burner open name`)
			AnsiConsole.WriteLine(project.Path);
		}

		return 0;
	}

	private void OpenInVSCode(string path)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "code",
				Arguments = $"\"{path}\"",
				UseShellExecute = true
			});
			AnsiConsole.MarkupLine($"[green]✓[/] Opened in VS Code: [blue]{path}[/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to open VS Code. Is it installed and in PATH?");
		}
	}

	private void OpenInExplorer(string path)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer",
				Arguments = $"\"{path}\"",
				UseShellExecute = true
			});
			AnsiConsole.MarkupLine($"[green]✓[/] Opened in Explorer: [blue]{path}[/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to open file explorer");
		}
	}
}
