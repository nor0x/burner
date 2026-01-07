using Burner.Models;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Diagnostics;

namespace Burner.Commands;

public class ConfigCommandSettings : CommandSettings
{
	[CommandOption("--home <PATH>")]
	[Description("Set the burner home directory")]
	public string? BurnerHome { get; set; }

	[CommandOption("--templates <PATH>")]
	[Description("Set the burner templates directory")]
	public string? BurnerTemplates { get; set; }

	[CommandOption("--auto-clean-days <DAYS>")]
	[Description("Set auto-cleanup age in days (0 to disable)")]
	public int? AutoCleanDays { get; set; }

	[CommandOption("--show")]
	[Description("Show current configuration")]
	public bool Show { get; set; }

	[CommandOption("--path")]
	[Description("Show config file path")]
	public bool ShowPath { get; set; }

	[CommandOption("--open-templates")]
	[Description("Open the templates directory in file explorer")]
	public bool OpenTemplates { get; set; }

	[CommandOption("--open-home")]
	[Description("Open the burner home directory in file explorer")]
	public bool OpenHome { get; set; }
}

public class ConfigCommand : Command<ConfigCommandSettings>
{
	public override int Execute(CommandContext context, ConfigCommandSettings settings)
	{
		var config = BurnerConfig.Load();

		if (settings.ShowPath)
		{
			AnsiConsole.MarkupLine($"[grey]Config file:[/] {BurnerConfig.GetConfigPath()}");
			return 0;
		}

		if (settings.OpenTemplates)
		{
			OpenDirectory(config.BurnerTemplates, "burner templates");
			return 0;
		}

		if (settings.OpenHome)
		{
			OpenDirectory(config.BurnerHome, "burner home");
			return 0;
		}

		var hasChanges = false;

		if (settings.BurnerHome != null)
		{
			config.BurnerHome = Path.GetFullPath(settings.BurnerHome);
			hasChanges = true;
			AnsiConsole.MarkupLine($"[green]✓[/] Set burner home to [blue]{config.BurnerHome}[/]");
		}

		if (settings.BurnerTemplates != null)
		{
			config.BurnerTemplates = Path.GetFullPath(settings.BurnerTemplates);
			hasChanges = true;
			AnsiConsole.MarkupLine($"[green]✓[/] Set burner templates to [blue]{config.BurnerTemplates}[/]");
		}

		if (settings.AutoCleanDays.HasValue)
		{
			config.AutoCleanDays = settings.AutoCleanDays.Value;
			hasChanges = true;
			if (config.AutoCleanDays > 0)
				AnsiConsole.MarkupLine($"[green]✓[/] Set auto-clean to [blue]{config.AutoCleanDays} days[/]");
			else
				AnsiConsole.MarkupLine($"[green]✓[/] Disabled auto-clean");
		}

		if (hasChanges)
		{
			config.Save();
		}

		// Show config if requested or no changes were made
		if (settings.Show || !hasChanges)
		{
			ShowConfig(config);
		}

		return 0;
	}

	private void ShowConfig(BurnerConfig config)
	{
		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule("🔥 Configuration"));
		AnsiConsole.WriteLine();

		var table = Banner.CreateTable("Setting", "Value");

		table.AddRow("[white]Burner Home[/]", $"[blue]{config.BurnerHome}[/]");
		table.AddRow("[white]Burner Templates[/]", $"[blue]{config.BurnerTemplates}[/]");
		table.AddRow("[white]Auto-Clean Days[/]", config.AutoCleanDays > 0
			? $"[yellow]{config.AutoCleanDays}[/]"
			: "[grey]disabled[/]");

		AnsiConsole.Write(table);
		AnsiConsole.MarkupLine($"\n[grey]Config file:[/] [dim]{BurnerConfig.GetConfigPath()}[/]");
	}

	private void OpenDirectory(string path, string label)
	{
		if (!Directory.Exists(path))
		{
			Directory.CreateDirectory(path);
			AnsiConsole.MarkupLine($"[yellow]Created {label} directory:[/] {path}");
		}

		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = "explorer",
				Arguments = $"\"{path}\"",
				UseShellExecute = true
			});
			AnsiConsole.MarkupLine($"[green]✓[/] Opened {label} directory: [blue]{path}[/]");
		}
		catch
		{
			AnsiConsole.MarkupLine($"[red]✗[/] Failed to open directory");
		}
	}
}
