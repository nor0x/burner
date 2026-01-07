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
		EnsureBuiltInTemplatesExist();
	}

	public IEnumerable<string> GetAvailableTemplates()
	{
		EnsureBuiltInTemplatesExist();

		var templates = new List<string>();

		if (Directory.Exists(_config.BurnerTemplates))
		{
			var customTemplates = Directory.GetFiles(_config.BurnerTemplates)
				.Where(f => IsExecutable(f))
				.Select(f => Path.GetFileNameWithoutExtension(f));
			templates.AddRange(customTemplates);
		}

		return templates.Distinct();
	}

	public IEnumerable<(string Name, string Filename, bool IsBuiltIn)> GetAvailableTemplatesWithDetails()
	{
		EnsureBuiltInTemplatesExist();

		var builtIn = new[] { "dotnet", "web" };
		var templates = new List<(string Name, string Filename, bool IsBuiltIn)>();

		if (Directory.Exists(_config.BurnerTemplates))
		{
			var files = Directory.GetFiles(_config.BurnerTemplates)
				.Where(f => IsExecutable(f));

			foreach (var file in files)
			{
				var name = Path.GetFileNameWithoutExtension(file);
				var filename = Path.GetFileName(file);
				var isBuiltIn = builtIn.Contains(name, StringComparer.OrdinalIgnoreCase);
				templates.Add((name, filename, isBuiltIn));
			}
		}

		return templates.DistinctBy(t => t.Name);
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

		// All templates now go through the custom template runner
		return CreateCustomProject(template, projectPath, projectName);
	}

	private bool CreateCustomProject(string template, string projectPath, string projectName)
	{
		EnsureBuiltInTemplatesExist();

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

	private void EnsureBuiltInTemplatesExist()
	{
		Directory.CreateDirectory(_config.BurnerTemplates);

		var extension = OperatingSystem.IsWindows() ? ".ps1" : ".sh";

		// Create dotnet template if it doesn't exist
		var dotnetPath = Path.Combine(_config.BurnerTemplates, $"dotnet{extension}");
		if (!File.Exists(dotnetPath))
		{
			File.WriteAllText(dotnetPath, GetDotnetTemplateScript(extension));
		}

		// Create web template if it doesn't exist
		var webPath = Path.Combine(_config.BurnerTemplates, $"web{extension}");
		if (!File.Exists(webPath))
		{
			File.WriteAllText(webPath, GetWebTemplateScript(extension));
		}
	}

	private static string GetDotnetTemplateScript(string extension)
	{
		if (extension == ".ps1")
		{
			return """
				#!/usr/bin/env pwsh
				# Burner built-in template: .NET Console Application
				param(
				    [Parameter(Mandatory=$true, Position=0)]
				    [string]$ProjectName,
				    [Parameter(Mandatory=$true, Position=1)]
				    [string]$TargetDirectory
				)
				$ErrorActionPreference = "Stop"
				Push-Location $TargetDirectory
				try {
				    dotnet new console -n $ProjectName -o . --force
				    if ($LASTEXITCODE -ne 0) { exit 1 }
				}
				finally { Pop-Location }
				""";
		}
		else
		{
			return """
				#!/bin/bash
				# Burner built-in template: .NET Console Application
				set -e
				PROJECT_NAME="$1"
				TARGET_DIRECTORY="$2"
				cd "$TARGET_DIRECTORY"
				dotnet new console -n "$PROJECT_NAME" -o . --force
				""";
		}
	}

	private static string GetWebTemplateScript(string extension)
	{
		if (extension == ".ps1")
		{
			return """
				#!/usr/bin/env pwsh
				# Burner built-in template: HTML + JS + CSS Web App
				param(
				    [Parameter(Mandatory=$true, Position=0)]
				    [string]$ProjectName,
				    [Parameter(Mandatory=$true, Position=1)]
				    [string]$TargetDirectory
				)
				$ErrorActionPreference = "Stop"

				$indexHtml = @"
				<!DOCTYPE html>
				<html lang="en">
				<head>
				    <meta charset="UTF-8">
				    <meta name="viewport" content="width=device-width, initial-scale=1.0">
				    <title>$ProjectName</title>
				    <link rel="stylesheet" href="styles.css">
				</head>
				<body>
				    <h1>$ProjectName</h1>
				    <p>Your web experiment starts here!</p>
				    <script src="script.js"></script>
				</body>
				</html>
				"@

				$stylesCss = @"
				* { margin: 0; padding: 0; box-sizing: border-box; }
				body {
				    font-family: system-ui, -apple-system, sans-serif;
				    line-height: 1.6;
				    padding: 2rem;
				    max-width: 800px;
				    margin: 0 auto;
				    background: #1a1a2e;
				    color: #eee;
				}
				h1 { color: #ff6b35; margin-bottom: 1rem; }
				p { color: #aaa; }
				"@

				$scriptJs = @"
				console.log('$ProjectName loaded!');
				document.addEventListener('DOMContentLoaded', () => console.log('DOM ready'));
				"@

				Set-Content -Path (Join-Path $TargetDirectory "index.html") -Value $indexHtml
				Set-Content -Path (Join-Path $TargetDirectory "styles.css") -Value $stylesCss
				Set-Content -Path (Join-Path $TargetDirectory "script.js") -Value $scriptJs
				""";
		}
		else
		{
			return """
				#!/bin/bash
				# Burner built-in template: HTML + JS + CSS Web App
				set -e
				PROJECT_NAME="$1"
				TARGET_DIRECTORY="$2"

				cat > "$TARGET_DIRECTORY/index.html" << EOF
				<!DOCTYPE html>
				<html lang="en">
				<head>
				    <meta charset="UTF-8">
				    <meta name="viewport" content="width=device-width, initial-scale=1.0">
				    <title>$PROJECT_NAME</title>
				    <link rel="stylesheet" href="styles.css">
				</head>
				<body>
				    <h1>$PROJECT_NAME</h1>
				    <p>Your web experiment starts here!</p>
				    <script src="script.js"></script>
				</body>
				</html>
				EOF

				cat > "$TARGET_DIRECTORY/styles.css" << 'EOF'
				* { margin: 0; padding: 0; box-sizing: border-box; }
				body {
				    font-family: system-ui, -apple-system, sans-serif;
				    line-height: 1.6;
				    padding: 2rem;
				    max-width: 800px;
				    margin: 0 auto;
				    background: #1a1a2e;
				    color: #eee;
				}
				h1 { color: #ff6b35; margin-bottom: 1rem; }
				p { color: #aaa; }
				EOF

				cat > "$TARGET_DIRECTORY/script.js" << EOF
				console.log('$PROJECT_NAME loaded!');
				document.addEventListener('DOMContentLoaded', () => console.log('DOM ready'));
				EOF
				""";
		}
	}
}
