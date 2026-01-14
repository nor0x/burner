using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Burner.Commands;

public class ImportCommandSettings : CommandSettings
{
	[CommandOption("-n|--name <NAME>")]
	[Description("Custom name for the imported project (defaults to current folder name)")]
	public string? Name { get; set; }

	[CommandOption("-c|--copy")]
	[Description("Copy the folder instead of moving it")]
	public bool Copy { get; set; }

	[CommandOption("-f|--force")]
	[Description("Skip confirmation prompt when moving")]
	public bool Force { get; set; }
}

public class ImportCommand : Command<ImportCommandSettings>
{
	public override int Execute(CommandContext context, ImportCommandSettings settings, CancellationToken cancellationToken)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		// Get current directory
		var currentDir = Directory.GetCurrentDirectory();
		var currentDirInfo = new DirectoryInfo(currentDir);
		var currentFolderName = currentDirInfo.Name;

		// Use provided name or default to current folder name
		var projectName = settings.Name ?? currentFolderName;

		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[yellow]Importing:[/] {currentFolderName}");
		AnsiConsole.MarkupLine($"[grey]Source:[/] {currentDir}");

		// Show confirmation when moving (unless force flag is used)
		if (!settings.Copy && !settings.Force)
		{
			AnsiConsole.WriteLine();
			AnsiConsole.MarkupLine($"[yellow]⚠[/] The current folder will be [red]moved[/] to the burner home directory.");
			AnsiConsole.MarkupLine($"[grey]After import, the folder will no longer be at:[/] {currentDir}");
			AnsiConsole.WriteLine();
			
			var confirm = AnsiConsole.Confirm("Do you want to proceed?", defaultValue: false);
			if (!confirm)
			{
				AnsiConsole.MarkupLine("[yellow]Import cancelled.[/]");
				return 0;
			}
		}

		// Import the project
		bool success = false;
		string? importedPath = null;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start($"[orangered1]Importing[/] project [white]{projectName}[/]...", ctx =>
			{
				var result = projectService.ImportProject(currentDir, projectName, settings.Copy);
				success = result.Success;
				importedPath = result.Path;
			});

		if (success && importedPath != null)
		{
			var operation = settings.Copy ? "copied" : "moved";
			AnsiConsole.MarkupLine($"[green]:fire:[/] Project {operation} to [blue]{importedPath}[/]");
			AnsiConsole.MarkupLine($"[grey]Template:[/] custom");
			
			if (!settings.Copy)
			{
				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine("[yellow]Note:[/] The original folder has been moved. Your current directory may no longer exist.");
				AnsiConsole.MarkupLine("[grey]Tip:[/] Use 'cd ~' or navigate to a different directory.");
			}
		}
		else
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to import project");
		}

		return success ? 0 : 1;
	}
}
