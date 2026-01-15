using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Burner.Commands;

/// <summary>
/// Lists all available project templates.
/// </summary>
public class TemplatesCommand : Command
{
	/// <inheritdoc/>
	public override int Execute(CommandContext context, CancellationToken cancellationToken)
	{
		var config = BurnerConfig.Load();
		var templateService = new TemplateService(config);

		ShowTemplatesTable(templateService, config);

		return 0;
	}

	/// <summary>
	/// Displays a formatted table of available templates.
	/// </summary>
	/// <param name="templateService">The template service to query.</param>
	/// <param name="config">The burner configuration.</param>
	public static void ShowTemplatesTable(TemplateService templateService, BurnerConfig config)
	{
		var templates = templateService.GetAvailableTemplatesWithDetails().ToList();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule(":fire: Templates"));
		AnsiConsole.WriteLine();

		var table = Banner.CreateTable("Template", "Filename", "Type");

		foreach (var template in templates)
		{
			var type = template.IsBuiltIn ? "[green]built-in[/]" : "[blue]custom[/]";
			table.AddRow(template.Name, $"[grey]{template.Filename}[/]", type);
		}

		AnsiConsole.Write(table);

		AnsiConsole.MarkupLine($"\n[grey]Templates directory:[/] [dim]{config.BurnerTemplates}[/]");
	}
}
