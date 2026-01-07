using Burner.Commands;
using Spectre.Console.Cli;

var app = new CommandApp<WelcomeCommand>();

app.Configure(config =>
{
	config.SetApplicationName("burner");
	config.SetApplicationVersion("1.0.0");

	config.AddCommand<NewCommand>("new")
		.WithDescription("Create a new burner project")
		.WithExample("new", "dotnet", "my-experiment")
		.WithExample("new", "web", "quick-test", "-d", "C:\\Projects");

	config.AddCommand<ListCommand>("list")
		.WithAlias("ls")
		.WithDescription("List all burner projects")
		.WithExample("list")
		.WithExample("list", "-a");

	config.AddCommand<BurnCommand>("burn")
		.WithAlias("rm")
		.WithDescription("Delete projects (cleanup)")
		.WithExample("burn", "--days", "30")
		.WithExample("burn", "my-experiment")
		.WithExample("burn", "-f", "--days", "7");

	config.AddCommand<OpenCommand>("open")
		.WithDescription("Open a project directory")
		.WithExample("open", "my-experiment")
		.WithExample("open", "my-experiment", "-c")
		.WithExample("open", "my-experiment", "-e");

	config.AddCommand<ConfigCommand>("config")
		.WithDescription("Configure burner settings")
		.WithExample("config", "--show")
		.WithExample("config", "--home", "C:\\MyProjects")
		.WithExample("config", "--auto-clean-days", "60");

	config.AddCommand<TemplatesCommand>("templates")
		.WithDescription("List available templates");

	config.AddCommand<StatsCommand>("stats")
		.WithDescription("Show project statistics and charts");
});

return app.Run(args);
