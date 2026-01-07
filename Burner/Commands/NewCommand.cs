using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Burner.Commands;

public class NewCommandSettings : CommandSettings
{
	[CommandArgument(0, "<TEMPLATE>")]
	[Description("Template to use (dotnet, web, or custom)")]
	public string Template { get; set; } = string.Empty;

	[CommandArgument(1, "<NAME>")]
	[Description("Project name")]
	public string Name { get; set; } = string.Empty;

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

		var availableTemplates = templateService.GetAvailableTemplates().ToList();
		if (!availableTemplates.Contains(settings.Template, StringComparer.OrdinalIgnoreCase))
		{
			AnsiConsole.MarkupLine($"[red]Unknown template:[/] {settings.Template}");
			AnsiConsole.MarkupLine($"[grey]Available templates:[/] {string.Join(", ", availableTemplates)}");
			return 1;
		}

		AnsiConsole.WriteLine();
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start($"[orangered1]Igniting[/] [yellow]{settings.Template}[/] project [white]{settings.Name}[/]...", ctx =>
			{
				var success = templateService.CreateProject(settings.Template, settings.Name, settings.Directory);
				if (success)
				{
					var projectName = $"{DateTime.Now:yyMMdd}-{settings.Name}";
					var projectPath = Path.Combine(settings.Directory ?? config.BurnerHome, projectName);
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
