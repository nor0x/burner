using Spectre.Console;
using Spectre.Console.Cli;

namespace Burner.Commands;

public class WelcomeCommand : Command
{
    public override int Execute(CommandContext context)
    {
        Banner.Show();
        
        AnsiConsole.Write(Banner.CreateRule("Quick Start"));
        AnsiConsole.WriteLine();
        
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        
        grid.AddRow("[orangered1]burner new dotnet myapp[/]", "[grey]Create a .NET console app[/]");
        grid.AddRow("[orangered1]burner new web mysite[/]", "[grey]Create an HTML/JS/CSS project[/]");
        grid.AddRow("[orangered1]burner list[/]", "[grey]List all projects[/]");
        grid.AddRow("[orangered1]burner burn --days 30[/]", "[grey]Clean up old projects[/]");
        grid.AddRow("[orangered1]burner --help[/]", "[grey]Show all commands[/]");
        
        AnsiConsole.Write(grid);
        AnsiConsole.WriteLine();

        return 0;
    }
}
