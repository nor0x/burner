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

		// Get the destination path first
		var destinationPath = projectService.GetImportDestinationPath(projectName);
		if (destinationPath == null)
		{
			AnsiConsole.MarkupLine("[red]✗[/] A project with this name already exists");
			return 1;
		}

		// Create destination directory and navigate to it first
		// This avoids issues with moving a directory we're currently in
		Directory.CreateDirectory(destinationPath);
		Directory.SetCurrentDirectory(destinationPath);

		// Import the project
		bool success = false;

		AnsiConsole.Status()
			.Spinner(Spinner.Known.Aesthetic)
			.SpinnerStyle(Style.Parse("orangered1"))
			.Start($"[orangered1]Importing[/] project [white]{projectName}[/]...", ctx =>
			{
				var result = projectService.ImportProject(currentDir, destinationPath, settings.Copy);
				success = result.Success;
			});

		if (success)
		{
			var operation = settings.Copy ? "copied" : "moved";
			AnsiConsole.MarkupLine($"[green]:fire:[/] Project {operation} to [blue]{destinationPath}[/]");
			AnsiConsole.MarkupLine($"[grey]Template:[/] custom");

			if (!settings.Copy)
			{
				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[grey]Tip:[/] Run [blue]cd \"{destinationPath}\"[/] to navigate to your project.");
				AnsiConsole.MarkupLine($"[grey]Note:[/] The original folder is now empty and can be deleted.");
			}
		}
		else
		{
			AnsiConsole.MarkupLine("[red]✗[/] Failed to import project");
		}

		return success ? 0 : 1;
	}
}
