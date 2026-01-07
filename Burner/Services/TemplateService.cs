using System.Diagnostics;
using Burner.Models;
using Spectre.Console;

namespace Burner.Services;

public class TemplateService
{
	private readonly BurnerConfig _config;

	public TemplateService(BurnerConfig config)
	{
		_config = config;
	}

	public IEnumerable<string> GetAvailableTemplates()
	{
		var templates = new List<string> { "dotnet", "web" };

		if (Directory.Exists(_config.BurnerTemplates))
		{
			var customTemplates = Directory.GetFiles(_config.BurnerTemplates)
				.Where(f => IsExecutable(f))
				.Select(f => Path.GetFileNameWithoutExtension(f));
			templates.AddRange(customTemplates);
		}

		return templates.Distinct();
	}

	public bool CreateProject(string template, string name, string? targetDirectory = null)
	{
		var projectDir = targetDirectory ?? _config.BurnerHome;
		var projectName = $"{DateTime.Now:yyMMdd}-{name}";
		var projectPath = Path.Combine(projectDir, projectName);

		Directory.CreateDirectory(projectDir);

		if (Directory.Exists(projectPath))
		{
			AnsiConsole.MarkupLine($"[red]Project already exists:[/] {projectPath}");
			return false;
		}

		return template.ToLower() switch
		{
			"dotnet" => CreateDotnetProject(projectPath, projectName),
			"web" => CreateWebProject(projectPath, projectName),
			_ => CreateCustomProject(template, projectPath, projectName)
		};
	}

	private bool CreateDotnetProject(string projectPath, string projectName)
	{
		Directory.CreateDirectory(projectPath);

		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"new console -n {projectName} -o .",
				WorkingDirectory = projectPath,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			}
		};

		process.Start();
		process.WaitForExit();

		return process.ExitCode == 0;
	}

	private bool CreateWebProject(string projectPath, string projectName)
	{
		Directory.CreateDirectory(projectPath);

		var indexHtml = $@"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0"">
    <title>{projectName}</title>
    <link rel=""stylesheet"" href=""styles.css"">
</head>
<body>
    <h1>{projectName}</h1>
    <p>Your web experiment starts here!</p>
    <script src=""script.js""></script>
</body>
</html>";

		var stylesCss = @"* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: system-ui, -apple-system, sans-serif;
    padding: 2rem;
    max-width: 800px;
    margin: 0 auto;
}

h1 {
    margin-bottom: 1rem;
    color: #333;
}
";

		var scriptJs = @"// Your JavaScript code here
console.log('Hello from your burner project!');
";

		File.WriteAllText(Path.Combine(projectPath, "index.html"), indexHtml);
		File.WriteAllText(Path.Combine(projectPath, "styles.css"), stylesCss);
		File.WriteAllText(Path.Combine(projectPath, "script.js"), scriptJs);

		return true;
	}

	private bool CreateCustomProject(string template, string projectPath, string projectName)
	{
		var scriptPath = FindTemplateScript(template);
		if (scriptPath == null)
		{
			AnsiConsole.MarkupLine($"[red]Template not found:[/] {template}");
			return false;
		}

		Directory.CreateDirectory(projectPath);

		var process = new Process
		{
			StartInfo = new ProcessStartInfo
			{
				FileName = scriptPath,
				Arguments = $"\"{projectName}\" \"{projectPath}\"",
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false
			}
		};

		// Handle different script types on Windows
		var extension = Path.GetExtension(scriptPath).ToLower();
		if (extension == ".ps1")
		{
			process.StartInfo.FileName = "pwsh";
			process.StartInfo.Arguments = $"-File \"{scriptPath}\" \"{projectName}\" \"{projectPath}\"";
		}
		else if (extension == ".py")
		{
			process.StartInfo.FileName = "python";
			process.StartInfo.Arguments = $"\"{scriptPath}\" \"{projectName}\" \"{projectPath}\"";
		}

		process.Start();
		process.WaitForExit();

		return process.ExitCode == 0;
	}

	private string? FindTemplateScript(string template)
	{
		if (!Directory.Exists(_config.BurnerTemplates))
			return null;

		var files = Directory.GetFiles(_config.BurnerTemplates);
		return files.FirstOrDefault(f =>
			Path.GetFileNameWithoutExtension(f).Equals(template, StringComparison.OrdinalIgnoreCase) &&
			IsExecutable(f));
	}

	private static bool IsExecutable(string path)
	{
		var ext = Path.GetExtension(path).ToLower();
		return ext is ".ps1" or ".bat" or ".cmd" or ".exe" or ".sh" or ".py";
	}
}
