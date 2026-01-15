using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace Burner.Commands;

/// <summary>
/// Settings for the open command.
/// </summary>
public class OpenCommandSettings : CommandSettings
{
	/// <summary>
	/// Project name to open (optional, interactive if not provided).
	/// </summary>
	[CommandArgument(0, "[NAME]")]
	[Description("Project name to open (optional, interactive if not provided)")]
	public string? Name { get; set; }

	/// <summary>
	/// Open in file explorer instead of terminal.
	/// </summary>
	[CommandOption("-e|--explorer")]
	[Description("Open in file explorer instead of terminal")]
	public bool Explorer { get; set; }

	/// <summary>
	/// Open in editor (uses configured editor).
	/// </summary>
	[CommandOption("-c|--code")]
	[Description("Open in editor (uses configured editor)")]
	public bool Code { get; set; }

	/// <summary>
	/// Interactive mode: select project from list.
	/// </summary>
	[CommandOption("-i|--interactive")]
	[Description("Interactive mode: select project from list")]
	public bool Interactive { get; set; }
}

/// <summary>
/// Opens a burner project in editor, file explorer, or outputs the path.
/// </summary>
public class OpenCommand : Command<OpenCommandSettings>
{
	public override int Execute(CommandContext context, OpenCommandSettings settings, CancellationToken cancellationToken)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		// Interactive mode if no name provided or -i flag
		if (string.IsNullOrEmpty(settings.Name) || settings.Interactive)
		{
			return InteractiveOpen(projectService, config);
		}

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
			OpenInEditor(project.Path, config.Editor);
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

	private int InteractiveOpen(ProjectService projectService, BurnerConfig config)
	{
		var projects = projectService.GetAllProjects().ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.WriteLine();
			var panel = new Panel("[grey]No burner projects found.\nCreate one with:[/] [yellow]burner new <template> <name>[/]")
				.Header(Emoji.Replace("[orangered1]:fire: Open[/]"))
				.Border(BoxBorder.Rounded)
				.BorderStyle(Style.Parse("grey"));
			AnsiConsole.Write(panel);
			return 0;
		}

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule(":fire: Select Project to Open"));
		AnsiConsole.WriteLine();

		// Select project
		var projectChoices = projects.Select(p =>
		{
			var ageColor = p.AgeInDays switch
			{
				< 7 => "green",
				< 30 => "yellow",
				_ => "red"
			};
			var ageText = p.AgeInDays switch
			{
				0 => "today",
				1 => "1 day",
				_ => $"{p.AgeInDays} days"
			};
			return $"{p.Name} [{ageColor}]({ageText})[/] [grey]- {p.Template}[/]";
		}).ToList();

		var selected = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title("[orangered1]Select a project:[/]")
				.PageSize(15)
				.HighlightStyle(Style.Parse("orangered1"))
				.AddChoices(projectChoices));

		// Extract project name
		var projectName = selected.Split(' ')[0];
		var project = projects.First(p => p.Name == projectName);

		AnsiConsole.WriteLine();

		// Select action
		var action = AnsiConsole.Prompt(
			new SelectionPrompt<string>()
				.Title($"[orangered1]How do you want to open[/] [blue]{project.Name}[/][orangered1]?[/]")
				.HighlightStyle(Style.Parse("orangered1"))
				.AddChoices(new[]
				{
					$":pencil: Open in editor ({config.Editor})",
					":file_folder: Open in file explorer",
					":clipboard: Copy path to clipboard"
				}));

		if (action.Contains("editor"))
		{
			OpenInEditor(project.Path, config.Editor);
		}
		else if (action.Contains("explorer"))
		{
			OpenInExplorer(project.Path);
		}
		else
		{
			// Copy to clipboard
			CopyToClipboard(project.Path);
		}

		return 0;
	}

	private void OpenInEditor(string path, string editor)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = editor,
				Arguments = $"\"{path}\"",
				UseShellExecute = true
			});
			AnsiConsole.MarkupLine($"[green]✓[/] Opened in {editor}: [blue]{path}[/]");
		}
		catch
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to open {editor}. Is it installed and in PATH?");
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

	private void CopyToClipboard(string path)
	{
		try
		{
			if (OperatingSystem.IsWindows())
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "cmd",
					Arguments = $"/c echo {path}| clip",
					UseShellExecute = false,
					CreateNoWindow = true
				})?.WaitForExit();
			}
			else if (OperatingSystem.IsMacOS())
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "pbcopy",
					RedirectStandardInput = true,
					UseShellExecute = false
				});
			}
			else
			{
				// Linux - try xclip
				var process = Process.Start(new ProcessStartInfo
				{
					FileName = "xclip",
					Arguments = "-selection clipboard",
					RedirectStandardInput = true,
					UseShellExecute = false
				});
				process?.StandardInput.Write(path);
				process?.StandardInput.Close();
				process?.WaitForExit();
			}
			AnsiConsole.MarkupLine($"[green]✓[/] Copied to clipboard: [blue]{path}[/]");
		}
		catch
		{
			// Fallback: just print the path
			AnsiConsole.MarkupLine($"[yellow]Path:[/] {path}");
		}
	}
}
