using Spectre.Console;
using Spectre.Console.Cli;
using System.Reflection;

namespace Burner.Commands;

public class VersionCommand : Command
{
	public override int Execute(CommandContext context, CancellationToken cancellationToken)
	{
		var assembly = Assembly.GetExecutingAssembly();
		var version = assembly.GetName().Version?.ToString(3) ?? "1.0.0";
		var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;

		// Strip any +commit hash suffix for cleaner display
		var displayVersion = informationalVersion.Split('+')[0];

		Banner.Show();

		var table = new Table()
			.Border(TableBorder.Rounded)
			.BorderStyle(Style.Parse("grey"))
			.AddColumn(new TableColumn("[grey]Property[/]").LeftAligned())
			.AddColumn(new TableColumn("[grey]Value[/]").LeftAligned());

		table.AddRow("[yellow]Version[/]", $"[white]{displayVersion}[/]");
		table.AddRow("[yellow]Runtime[/]", $"[white]{System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}[/]");
		table.AddRow("[yellow]OS[/]", $"[white]{System.Runtime.InteropServices.RuntimeInformation.OSDescription}[/]");
		table.AddRow("[yellow]Architecture[/]", $"[white]{System.Runtime.InteropServices.RuntimeInformation.OSArchitecture}[/]");
		table.AddRow("[yellow]Repository[/]", "[link=https://github.com/nor0x/burner]https://github.com/nor0x/burner[/]");
		table.AddRow("[yellow]Author[/]", "[white]Joachim Leonfellner[/]");
		table.AddRow("[yellow]License[/]", "[white]MIT[/]");

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();

		return 0;
	}
}
