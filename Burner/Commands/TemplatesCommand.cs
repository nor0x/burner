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

		var templates = templateService.GetAvailableTemplates().ToList();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule(":fire: Templates"));
		AnsiConsole.WriteLine();

		var table = Banner.CreateTable("Template", "Type");

		var builtIn = new[] { "dotnet", "web" };

		foreach (var template in templates)
		{
			var type = builtIn.Contains(template) ? "[green]built-in[/]" : "[blue]custom[/]";
			table.AddRow(template, type);
		}

		AnsiConsole.Write(table);

		if (!Directory.Exists(config.BurnerTemplates))
		{
			AnsiConsole.MarkupLine($"\n[grey]Burner templates directory:[/] {config.BurnerTemplates}");
			AnsiConsole.MarkupLine("[grey]Create this directory and add scripts to define custom templates.[/]");
		}

		return 0;
	}
}
