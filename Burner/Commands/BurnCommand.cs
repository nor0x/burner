using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Burner.Commands;

public class BurnCommandSettings : CommandSettings
{
	[CommandArgument(0, "[NAME]")]
	[Description("Specific project name to delete (optional)")]
	public string? Name { get; set; }

	[CommandOption("-d|--days <DAYS>")]
	[Description("Delete projects older than specified days")]
	public int? Days { get; set; }

	[CommandOption("-f|--force")]
	[Description("Skip confirmation prompt")]
	public bool Force { get; set; }

	[CommandOption("-i|--interactive")]
	[Description("Interactive mode: select projects to delete")]
	public bool Interactive { get; set; }

	[CommandOption("-a|--all")]
	[Description("Delete ALL burner projects")]
	public bool All { get; set; }
}

public class BurnCommand : Command<BurnCommandSettings>
{
	public override int Execute(CommandContext context, BurnCommandSettings settings, CancellationToken cancellationToken)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		// Burn all projects
		if (settings.All)
		{
			return BurnAll(projectService, settings.Force);
		}

		// Interactive mode
		if (settings.Interactive)
		{
			return InteractiveDelete(projectService);
		}

		// Delete specific project
		if (!string.IsNullOrEmpty(settings.Name))
		{
			return DeleteSpecificProject(projectService, settings.Name, settings.Force);
		}

		// Cleanup old projects
		return CleanupOldProjects(projectService, config, settings.Days, settings.Force);
	}

	private int InteractiveDelete(ProjectService projectService)
	{
		var projects = projectService.GetAllProjects().ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.WriteLine();
			var panel = new Panel("[grey]No burner projects to delete.[/]")
				.Header(Emoji.Replace("[orangered1]:fire: Burn[/]"))
				.Border(BoxBorder.Rounded)
				.BorderStyle(Style.Parse("grey"));
			AnsiConsole.Write(panel);
			return 0;
		}

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule(":fire: Select Projects to Burn"));
		AnsiConsole.WriteLine();

		var selected = AnsiConsole.Prompt(
			new MultiSelectionPrompt<string>()
				.Title("[orangered1]Select projects to delete:[/]")
				.NotRequired()
				.PageSize(15)
				.HighlightStyle(Style.Parse("orangered1"))
				.InstructionsText("[grey](Press [blue]<space>[/] to toggle, [green]<enter>[/] to confirm)[/]")
				.AddChoices(projects.Select(p =>
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
				})));

		if (selected.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]No projects selected.[/]");
			return 0;
		}

		// Extract project names from selection (remove the age/template suffix)
		var projectNames = selected.Select(s => s.Split(' ')[0]).ToList();

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[yellow]Selected {projectNames.Count} project(s) for deletion:[/]");
		foreach (var name in projectNames)
		{
			AnsiConsole.MarkupLine($"  [orangered1]:fire:[/] [blue]{name}[/]");
		}

		var confirm = AnsiConsole.Confirm($"\n[orangered1]Permanently delete these projects?[/]", defaultValue: false);
		if (!confirm)
		{
			AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
			return 0;
		}

		var deleted = 0;
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start("[orangered1]Burning projects...[/]", ctx =>
			{
				foreach (var name in projectNames)
				{
					if (projectService.DeleteProject(name))
					{
						deleted++;
					}
				}
			});

		AnsiConsole.MarkupLine($"\n[green]:fire:[/] Burned [orangered1]{deleted}[/] project(s)");
		return 0;
	}

	private int DeleteSpecificProject(ProjectService projectService, string name, bool force)
	{
		var project = projectService.GetProject(name);
		if (project == null)
		{
			AnsiConsole.MarkupLine($"[red]Project not found:[/] {name}");
			return 1;
		}

		if (!force)
		{
			var confirm = AnsiConsole.Confirm($"Delete project [blue]{project.Name}[/]?", defaultValue: false);
			if (!confirm)
			{
				AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
				return 0;
			}
		}

		if (projectService.DeleteProject(name))
		{
			AnsiConsole.MarkupLine($"[green]✓[/] Deleted [blue]{project.Name}[/]");
			return 0;
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to delete project");
			return 1;
		}
	}

	private int CleanupOldProjects(ProjectService projectService, BurnerConfig config, int? days, bool force)
	{
		var cleanupDays = days ?? config.AutoCleanDays;

		if (cleanupDays <= 0)
		{
			AnsiConsole.MarkupLine("[yellow]Auto-cleanup is disabled.[/] Use [blue]--days[/] to specify cleanup age.");
			return 0;
		}

		var projects = projectService.GetAllProjects()
			.Where(p => p.AgeInDays > cleanupDays)
			.ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.MarkupLine($"[grey]No projects older than {cleanupDays} days.[/]");
			return 0;
		}

		AnsiConsole.MarkupLine($"[yellow]Found {projects.Count} project(s) older than {cleanupDays} days:[/]");
		foreach (var project in projects)
		{
			AnsiConsole.MarkupLine($"  [grey]•[/] [blue]{project.Name}[/] ({project.AgeInDays} days old)");
		}

		if (!force)
		{
			var confirm = AnsiConsole.Confirm($"\nDelete these {projects.Count} project(s)?", defaultValue: false);
			if (!confirm)
			{
				AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
				return 0;
			}
		}

		var deleted = projectService.CleanupProjects(cleanupDays);
		AnsiConsole.MarkupLine($"[green]✓[/] Deleted {deleted} project(s)");

		return 0;
	}

	private int BurnAll(ProjectService projectService, bool force)
	{
		var projects = projectService.GetAllProjects().ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]No burner projects to delete.[/]");
			return 0;
		}

		AnsiConsole.MarkupLine($"[yellow]Found {projects.Count} project(s):[/]");
		foreach (var project in projects)
		{
			AnsiConsole.MarkupLine($"  [orangered1]:fire:[/] [blue]{project.Name}[/] [grey]({project.AgeInDays} days old)[/]");
		}

		if (!force)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine("[bold red]WARNING: This will permanently delete ALL burner projects![/]");
			var confirm = AnsiConsole.Confirm($"[orangered1]Delete all {projects.Count} project(s)?[/]", defaultValue: false);
			if (!confirm)
			{
				AnsiConsole.MarkupLine("[grey]Cancelled.[/]");
				return 0;
			}
		}

		var deleted = 0;
		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start("[orangered1]Burning all projects...[/]", ctx =>
			{
				foreach (var project in projects)
				{
					if (projectService.DeleteProject(project.Name))
					{
						deleted++;
					}
				}
			});

		AnsiConsole.MarkupLine($"\n[green]:fire:[/] Burned [orangered1]{deleted}[/] project(s)");
		return 0;
	}
}
