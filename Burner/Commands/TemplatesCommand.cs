using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Burner.Commands;

public class TemplatesCommand : Command
{
	public override int Execute(CommandContext context)
	{
		var config = BurnerConfig.Load();
		var templateService = new TemplateService(config);

		ShowTemplatesTable(templateService, config);

		return 0;
	}

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
