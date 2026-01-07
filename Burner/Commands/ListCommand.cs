using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Burner.Commands;

public class ListCommandSettings : CommandSettings
{
	[CommandOption("-a|--all")]
	[Description("Show all details")]
	public bool ShowAll { get; set; }
}

public class ListCommand : Command<ListCommandSettings>
{
	public override int Execute(CommandContext context, ListCommandSettings settings)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		var projects = projectService.GetAllProjects().ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.WriteLine();
			var panel = new Panel("[grey]No burner projects found.\nCreate one with:[/] [yellow]burner new <template> <name>[/]")
				.Header("[orangered1]🔥 Projects[/]")
				.Border(BoxBorder.Rounded)
				.BorderStyle(Style.Parse("grey"));
			AnsiConsole.Write(panel);
			return 0;
		}

		var table = Banner.CreateTable("Name", "Template", "Age");

		if (settings.ShowAll)
		{
			table.AddColumn(new TableColumn("[orangered1]Path[/]"));
		}

		foreach (var project in projects)
		{
			var ageColor = project.AgeInDays switch
			{
				< 7 => "green",
				< 30 => "yellow",
				_ => "red"
			};

			var ageText = project.AgeInDays switch
			{
				0 => "today",
				1 => "1 day",
				_ => $"{project.AgeInDays} days"
			};

			if (settings.ShowAll)
			{
				table.AddRow(
					$"[blue]{project.Name}[/]",
					project.Template,
					$"[{ageColor}]{ageText}[/]",
					$"[grey]{project.Path}[/]"
				);
			}
			else
			{
				table.AddRow(
					$"[blue]{project.Name}[/]",
					project.Template,
					$"[{ageColor}]{ageText}[/]"
				);
			}
		}

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule("🔥 Projects"));
		AnsiConsole.WriteLine();
		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine($"\n[grey]Total:[/] [orangered1]{projects.Count}[/] [grey]project(s)[/]");

		return 0;
	}
}
