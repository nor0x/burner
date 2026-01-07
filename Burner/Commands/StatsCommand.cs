using Burner.Models;
using Burner.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Burner.Commands;

public class StatsCommand : Command
{
	public override int Execute(CommandContext context)
	{
		var config = BurnerConfig.Load();
		var projectService = new ProjectService(config);

		var projects = projectService.GetAllProjects().ToList();

		if (projects.Count == 0)
		{
			AnsiConsole.WriteLine();
			var panel = new Panel("[grey]No burner projects found. Create some first![/]")
				.Header(Emoji.Replace("[orangered1]:fire: Stats[/]"))
				.Border(BoxBorder.Rounded)
				.BorderStyle(Style.Parse("grey"));
			AnsiConsole.Write(panel);
			return 0;
		}

		AnsiConsole.WriteLine();
		AnsiConsole.Write(Banner.CreateRule(":fire: Statistics"));
		AnsiConsole.WriteLine();

		// Summary stats
		ShowSummary(projects, config);

		// By template
		AnsiConsole.WriteLine();
		ShowByTemplate(projects);

		// By age
		AnsiConsole.WriteLine();
		ShowByAge(projects, config);

		// By month
		AnsiConsole.WriteLine();
		ShowByMonth(projects);

		return 0;
	}

	private void ShowSummary(List<BurnerProject> projects, BurnerConfig config)
	{
		var totalSize = projects.Sum(p => GetDirectorySize(p.Path));
		var atRisk = projects.Count(p => p.AgeInDays > config.AutoCleanDays && config.AutoCleanDays > 0);
		var avgAge = projects.Average(p => p.AgeInDays);

		var grid = new Grid();
		grid.AddColumn();
		grid.AddColumn();
		grid.AddColumn();
		grid.AddColumn();

		grid.AddRow(
			new Markup($"[orangered1]Total Projects[/]\n[bold white]{projects.Count}[/]"),
			new Markup($"[orangered1]Disk Usage[/]\n[bold white]{FormatSize(totalSize)}[/]"),
			new Markup($"[orangered1]Avg Age[/]\n[bold white]{avgAge:F0} days[/]"),
			new Markup($"[orangered1]Due for Cleanup[/]\n[bold {(atRisk > 0 ? "yellow" : "white")}]{atRisk}[/]")
		);

		var panel = new Panel(grid)
			.Header("[orangered1]Overview[/]")
			.Border(BoxBorder.Rounded)
			.BorderStyle(Style.Parse("grey"));

		AnsiConsole.Write(panel);
	}

	private void ShowByTemplate(List<BurnerProject> projects)
	{
		var byTemplate = projects
			.GroupBy(p => p.Template)
			.Select(g => new { Template = g.Key, Count = g.Count() })
			.OrderByDescending(x => x.Count)
			.ToList();

		var chart = new BarChart()
			.Width(60)
			.Label("[orangered1]Projects by Template[/]");

		var colors = new[] { Color.OrangeRed1, Color.Orange1, Color.Yellow, Color.Red, Color.Grey };
		var colorIndex = 0;

		foreach (var item in byTemplate)
		{
			chart.AddItem(item.Template, item.Count, colors[colorIndex % colors.Length]);
			colorIndex++;
		}

		AnsiConsole.Write(chart);
	}

	private void ShowByAge(List<BurnerProject> projects, BurnerConfig config)
	{
		var fresh = projects.Count(p => p.AgeInDays < 7);
		var recent = projects.Count(p => p.AgeInDays >= 7 && p.AgeInDays < 30);
		var old = projects.Count(p => p.AgeInDays >= 30 && p.AgeInDays < 90);
		var ancient = projects.Count(p => p.AgeInDays >= 90);

		var chart = new BreakdownChart()
			.Width(60)
			.AddItem("< 7 days", fresh, Color.Green)
			.AddItem("7-30 days", recent, Color.Yellow)
			.AddItem("30-90 days", old, Color.Orange1)
			.AddItem("> 90 days", ancient, Color.Red);

		AnsiConsole.MarkupLine("[orangered1]Projects by Age[/]");
		AnsiConsole.WriteLine();
		AnsiConsole.Write(chart);

		if (config.AutoCleanDays > 0)
		{
			var atRisk = projects.Count(p => p.AgeInDays > config.AutoCleanDays);
			if (atRisk > 0)
			{
				AnsiConsole.WriteLine();
				AnsiConsole.MarkupLine($"[yellow]⚠ {atRisk} project(s) older than {config.AutoCleanDays} days (cleanup threshold)[/]");
			}
		}
	}

	private void ShowByMonth(List<BurnerProject> projects)
	{
		var byMonth = projects
			.GroupBy(p => new { p.CreatedAt.Year, p.CreatedAt.Month })
			.Select(g => new
			{
				Month = new DateTime(g.Key.Year, g.Key.Month, 1),
				Count = g.Count()
			})
			.OrderBy(x => x.Month)
			.TakeLast(6)
			.ToList();

		if (byMonth.Count < 2)
			return;

		var chart = new BarChart()
			.Width(60)
			.Label("[orangered1]Projects Created (Last 6 Months)[/]");

		foreach (var item in byMonth)
		{
			chart.AddItem(item.Month.ToString("MMM yy"), item.Count, Color.OrangeRed1);
		}

		AnsiConsole.Write(chart);
	}

	private long GetDirectorySize(string path)
	{
		try
		{
			if (!Directory.Exists(path))
				return 0;

			return new DirectoryInfo(path)
				.EnumerateFiles("*", SearchOption.AllDirectories)
				.Sum(f => f.Length);
		}
		catch
		{
			return 0;
		}
	}

	private string FormatSize(long bytes)
	{
		string[] sizes = ["B", "KB", "MB", "GB"];
		var order = 0;
		double size = bytes;

		while (size >= 1024 && order < sizes.Length - 1)
		{
			order++;
			size /= 1024;
		}

		return $"{size:F1} {sizes[order]}";
	}
}
