using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

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
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start($"[orangered1]Igniting[/] [yellow]{settings.Template}[/] project [white]{projectName}[/]...", ctx =>
			{
				var success = templateService.CreateProject(settings.Template, projectName, settings.Directory);
				if (success)
				{
					var fullProjectName = $"{DateTime.Now:yyMMdd}-{projectName}";
					var projectPath = Path.Combine(settings.Directory ?? config.BurnerHome, fullProjectName);
					AnsiConsole.MarkupLine($"[green]:fire:[/] Project created at [blue]{projectPath}[/]");
				}
				else
				{
					AnsiConsole.MarkupLine("[red]✗[/] Failed to create project");
				}
			});

		return 0;
	}
}
