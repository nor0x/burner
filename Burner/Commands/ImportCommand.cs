using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace Burner.Commands;

/// <summary>
/// Settings for the import command.
/// </summary>
public class ImportCommandSettings : CommandSettings
{
	/// <summary>
	/// Custom name for the imported project (defaults to current folder name).
	/// </summary>
	[CommandOption("-n|--name <NAME>")]
	[Description("Custom name for the imported project (defaults to current folder name)")]
	public string? Name { get; set; }

	/// <summary>
	/// Copy the folder instead of moving it.
	/// </summary>
	[CommandOption("-c|--copy")]
	[Description("Copy the folder instead of moving it")]
	public bool Copy { get; set; }

	/// <summary>
	/// Skip confirmation prompt when moving.
	/// </summary>
	[CommandOption("-f|--force")]
	[Description("Skip confirmation prompt when moving")]
	public bool Force { get; set; }
}

/// <summary>
/// Imports the current directory as a burner project.
/// </summary>
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

		// Validate project name for invalid filesystem characters
		var invalidChars = Path.GetInvalidFileNameChars();
		if (projectName.IndexOfAny(invalidChars) >= 0)
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Invalid project name. The name contains invalid characters.");
			AnsiConsole.MarkupLine($"[grey]Invalid characters:[/] {string.Join(" ", invalidChars.Select(c => c == '\0' ? "\\0" : c.ToString()))}");
			return 1;
		}

		// Check if we're already inside the burner home directory
		var burnerHomePath = Path.GetFullPath(config.BurnerHome);
		var currentDirPath = Path.GetFullPath(currentDir);
		if (currentDirPath.StartsWith(burnerHomePath, StringComparison.OrdinalIgnoreCase))
		{
			AnsiConsole.MarkupLine("[yellow]⚠[/] Current folder is already inside the burner home directory.");
			AnsiConsole.MarkupLine("[grey]No need to import.[/]");
			return 0;
		}

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

		// Import the project
		bool success = false;
		var originalDir = currentDir;
		var destinationCreated = false;

		try
		{
			// Create destination directory and navigate to it first
			// This avoids issues with moving a directory we're currently in
			Directory.CreateDirectory(destinationPath);
			destinationCreated = true;

			// Only change directory for move operations
			if (!settings.Copy)
			{
				Directory.SetCurrentDirectory(destinationPath);
			}

			AnsiConsole.Status()
				.Spinner(Spinner.Known.Aesthetic)
				.SpinnerStyle(Style.Parse("orangered1"))
				.Start($"[orangered1]Importing[/] project [white]{projectName}[/]...", ctx =>
				{
					var result = projectService.ImportProject(currentDir, destinationPath, settings.Copy);
					success = result.Success;
				});
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to import project: {ex.Message}");
			success = false;
		}
		finally
		{
			// Restore working directory if we changed it and operation failed
			if (!settings.Copy && !success)
			{
				try
				{
					Directory.SetCurrentDirectory(originalDir);
				}
				catch
				{
					// If we can't restore, that's okay - the original directory may no longer exist
				}
			}

			// Clean up empty destination directory on failure
			if (!success && destinationCreated && Directory.Exists(destinationPath))
			{
				try
				{
					if (!Directory.EnumerateFileSystemEntries(destinationPath).Any())
					{
						Directory.Delete(destinationPath, recursive: false);
					}
				}
				catch
				{
					// If cleanup fails, that's okay - we tried
				}
			}
		}

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
