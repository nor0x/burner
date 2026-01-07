using Spectre.Console;

namespace Burner;

public static class Banner
{
    private const string LogoText = """
        [bold orangered1]██████╗ ██╗   ██╗██████╗ ███╗   ██╗███████╗██████╗[/]
        [bold orange1]██╔══██╗██║   ██║██╔══██╗████╗  ██║██╔════╝██╔══██╗[/]
        [bold yellow]██████╔╝██║   ██║██████╔╝██╔██╗ ██║█████╗  ██████╔╝[/]
        [bold orange1]██╔══██╗██║   ██║██╔══██╗██║╚██╗██║██╔══╝  ██╔══██╗[/]
        [bold orangered1]██████╔╝╚██████╔╝██║  ██║██║ ╚████║███████╗██║  ██║[/]
        [bold red]╚═════╝  ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═══╝╚══════╝╚═╝  ╚═╝[/]
        """;

    public static void Show()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Markup(LogoText);
        AnsiConsole.MarkupLine("  🔥");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Spark ideas. Burn when done.[/]");
        AnsiConsole.WriteLine();
    }

    public static void ShowMini()
    {
        AnsiConsole.MarkupLine("[orangered1]🔥 burner[/]");
    }

    public static Rule CreateRule(string title)
    {
        return new Rule($"[orangered1]{title}[/]")
            .RuleStyle(Style.Parse("grey"))
            .LeftJustified();
    }

    public static Panel CreatePanel(string content, string title)
    {
        return new Panel(content)
            .Header($"[orangered1]{title}[/]")
            .Border(BoxBorder.Rounded)
            .BorderStyle(Style.Parse("grey"));
    }

    public static Table CreateTable(params string[] columns)
    {
        var table = new Table()
            .Border(TableBorder.Rounded)
            .BorderStyle(Style.Parse("grey"));

        foreach (var column in columns)
        {
            table.AddColumn(new TableColumn($"[orangered1]{column}[/]"));
        }

        return table;
    }
}
