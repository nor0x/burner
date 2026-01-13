using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace Burner.Commands;

public class NewCommandSettings : CommandSettings
{
	[CommandArgument(0, "[TEMPLATE]")]
	[Description("Template to use (dotnet, web, or custom). Shows available templates if not provided.")]
	public string? Template { get; set; }

	[CommandArgument(1, "[NAME]")]
	[Description("Project name (auto-generated if not provided)")]
	public string? Name { get; set; }

	[CommandOption("-d|--directory <PATH>")]
	[Description("Target directory (uses default if not specified)")]
	public string? Directory { get; set; }

	[CommandOption("-e|--explorer")]
	[Description("Open in file explorer after creation")]
	public bool Explorer { get; set; }

	[CommandOption("-c|--code")]
	[Description("Open in editor after creation (uses configured editor)")]
	public bool Code { get; set; }

	[CommandOption("-i|--interactive")]
	[Description("Run template interactively (for templates requiring user input)")]
	public bool Interactive { get; set; }
}

public class NewCommand : Command<NewCommandSettings>
{
	public override int Execute(CommandContext context, NewCommandSettings settings)
	{
		var config = BurnerConfig.Load();
		var templateService = new TemplateService(config);

		// If no template provided, show available templates
		if (string.IsNullOrEmpty(settings.Template))
		{
			AnsiConsole.MarkupLine("[yellow]No template specified.[/] Select a template from the list below:\n");
			TemplatesCommand.ShowTemplatesTable(templateService, config);
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[grey]Usage:[/] burner new [grey][[template]][/] [grey][[name]][/]");
			AnsiConsole.MarkupLine("[grey]Example:[/] burner new dotnet my-experiment");
			return 0;
		}

		var availableTemplates = templateService.GetAvailableTemplates().ToList();
		if (!availableTemplates.Contains(settings.Template, StringComparer.OrdinalIgnoreCase))
		{
			AnsiConsole.MarkupLine($"[red]Unknown template:[/] {settings.Template}\n");
			TemplatesCommand.ShowTemplatesTable(templateService, config);
			return 1;
		}

		// Generate name if not provided: template-HHMMSS
		var projectName = settings.Name ?? $"{settings.Template}-{DateTime.Now:HHmmss}";

		AnsiConsole.WriteLine();

		var fullProjectName = $"{DateTime.Now:yyMMdd}-{projectName}";
		var projectPath = Path.Combine(settings.Directory ?? config.BurnerHome, fullProjectName);
		bool success;

		// Check if template requires interactive mode (via BURNER_INTERACTIVE marker)
		var isInteractive = settings.Interactive || templateService.IsInteractiveTemplate(settings.Template);

		if (isInteractive)
		{
			// Interactive mode: run without spinner to allow user input
			AnsiConsole.MarkupLine($"[orangered1]Igniting[/] [yellow]{settings.Template}[/] project [white]{projectName}[/] [grey](interactive mode)[/]...");
			AnsiConsole.WriteLine();
			success = templateService.CreateProject(settings.Template, projectName, settings.Directory, interactive: true);
		}
		else
		{
			success = false;
			AnsiConsole.Status()
				.Spinner(Spinner.Known.Aesthetic)
				.SpinnerStyle(Style.Parse("orangered1"))
				.Start($"[orangered1]Igniting[/] [yellow]{settings.Template}[/] project [white]{projectName}[/]...", ctx =>
				{
					success = templateService.CreateProject(settings.Template, projectName, settings.Directory, interactive: false);
				});
		}

		if (success)
		{
			AnsiConsole.MarkupLine($"[green]:fire:[/] Project created at [blue]{projectPath}[/]");

			if (settings.Code)
			{
				OpenInEditor(projectPath, config.Editor);
			}
			else if (settings.Explorer)
			{
				OpenInExplorer(projectPath);
			}
		}
		else
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to create project");
		}

		return success ? 0 : 1;
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
			if (OperatingSystem.IsWindows())
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "explorer",
					Arguments = $"\"{path}\"",
					UseShellExecute = true
				});
			}
			else if (OperatingSystem.IsMacOS())
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "open",
					Arguments = $"\"{path}\"",
					UseShellExecute = true
				});
			}
			else
			{
				Process.Start(new ProcessStartInfo
				{
					FileName = "xdg-open",
					Arguments = $"\"{path}\"",
					UseShellExecute = true
				});
			}
			AnsiConsole.MarkupLine($"[green]✓[/] Opened in file explorer: [blue]{path}[/]");
		}
		catch
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to open file explorer");
		}
	}
}
