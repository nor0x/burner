using System.Diagnostics;
using System.Reflection;
using Burner.Models;
using Spectre.Console;

namespace Burner.Services;

/// <summary>
/// Manages templates - listing, creating projects, and handling built-in/custom templates.
/// </summary>
public class TemplateService
{
	private readonly BurnerConfig _config;
	private readonly Assembly _assembly;

	/// <summary>
	/// Creates a new instance. Ensures built-in templates exist in the templates directory.
	/// </summary>
	/// <param name="config">The burner configuration to use.</param>
	public TemplateService(BurnerConfig config)
	{
		_config = config;
		_assembly = Assembly.GetExecutingAssembly();
		EnsureBuiltInTemplatesExist();
	}

	/// <summary>
	/// Returns template names (without file extensions).
	/// </summary>
	/// <returns>An enumerable of available template names.</returns>
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

	/// <summary>
	/// Returns templates with full details including filename and built-in status.
	/// </summary>
	/// <returns>An enumerable of tuples containing template name, filename, and built-in status.</returns>
	public IEnumerable<(string Name, string Filename, bool IsBuiltIn)> GetAvailableTemplatesWithDetails()
	{
		EnsureBuiltInTemplatesExist();

		var builtInNames = GetBuiltInTemplateNames();
		var templates = new List<(string Name, string Filename, bool IsBuiltIn)>();

		if (Directory.Exists(_config.BurnerTemplates))
		{
			var files = Directory.GetFiles(_config.BurnerTemplates)
				.Where(f => IsExecutable(f));

			foreach (var file in files)
			{
				var name = Path.GetFileNameWithoutExtension(file);
				var filename = Path.GetFileName(file);
				var isBuiltIn = builtInNames.Contains(name, StringComparer.OrdinalIgnoreCase);
				templates.Add((name, filename, isBuiltIn));
			}
		}

		return templates.DistinctBy(t => t.Name);
	}

	/// <summary>
	/// Checks if a template requires interactive mode by looking for BURNER_INTERACTIVE marker.
	/// </summary>
	/// <param name="template">The template name to check.</param>
	/// <returns>True if the template requires interactive mode.</returns>
	public bool IsInteractiveTemplate(string template)
	{
		EnsureBuiltInTemplatesExist();
		var scriptPath = FindTemplateScript(template);
		if (scriptPath == null) return false;

		try
		{
			var content = File.ReadAllText(scriptPath);
			// Look for BURNER_INTERACTIVE marker in first few lines
			var lines = content.Split('\n').Take(10);
			return lines.Any(line => line.Contains("BURNER_INTERACTIVE", StringComparison.OrdinalIgnoreCase));
		}
		catch
		{
			return false;
		}
	}

	/// <summary>
	/// Creates a new project from a template.
	/// </summary>
	/// <param name="template">The template name to use.</param>
	/// <param name="name">The project name.</param>
	/// <param name="targetDirectory">Optional target directory (uses burner home if not specified).</param>
	/// <param name="interactive">Whether to run in interactive mode for user input.</param>
	/// <returns>True if the project was created successfully.</returns>
	public bool CreateProject(string template, string name, string? targetDirectory = null, bool interactive = false)
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

		// Auto-detect interactive mode from template if not explicitly set
		var useInteractive = interactive || IsInteractiveTemplate(template);

		// All templates now go through the custom template runner
		return CreateCustomProject(template, projectPath, name, projectName, useInteractive);
	}

	private bool CreateCustomProject(string template, string projectPath, string simpleName, string datedName, bool interactive = false)
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
				WorkingDirectory = projectPath,
				// In interactive mode, don't redirect streams to allow user input
				RedirectStandardOutput = !interactive,
				RedirectStandardError = !interactive,
				RedirectStandardInput = !interactive,
				UseShellExecute = false
			}
		};

		// Expose environment variables for simpler template scripts
		process.StartInfo.Environment["BURNER_NAME"] = simpleName;
		process.StartInfo.Environment["BURNER_PATH"] = projectPath;
		process.StartInfo.Environment["BURNER_DATED_NAME"] = datedName;

		// Handle different script types
		var extension = Path.GetExtension(scriptPath).ToLower();
		if (extension == ".ps1")
		{
			process.StartInfo.FileName = "pwsh";
			process.StartInfo.Arguments = $"-File \"{scriptPath}\"";
		}
		else if (extension == ".py")
		{
			process.StartInfo.FileName = "python";
			process.StartInfo.Arguments = $"\"{scriptPath}\"";
		}
		else if (extension == ".sh")
		{
			process.StartInfo.FileName = "bash";
			process.StartInfo.Arguments = $"\"{scriptPath}\"";
		}
		else
		{
			process.StartInfo.FileName = scriptPath;
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
		var builtInTemplates = GetBuiltInTemplateNames();

		foreach (var templateName in builtInTemplates)
		{
			var targetPath = Path.Combine(_config.BurnerTemplates, $"{templateName}{extension}");
			if (!File.Exists(targetPath))
			{
				var content = GetEmbeddedTemplate(templateName, extension);
				if (content != null)
				{
					File.WriteAllText(targetPath, content);
				}
			}
		}
	}

	private IEnumerable<string> GetBuiltInTemplateNames()
	{
		var prefix = "Burner.Templates.";
		var extension = OperatingSystem.IsWindows() ? ".ps1" : ".sh";

		return _assembly.GetManifestResourceNames()
			.Where(n => n.StartsWith(prefix) && n.EndsWith(extension))
			.Select(n => n[prefix.Length..^extension.Length])
			.Distinct();
	}

	private string? GetEmbeddedTemplate(string name, string extension)
	{
		var resourceName = $"Burner.Templates.{name}{extension}";
		using var stream = _assembly.GetManifestResourceStream(resourceName);
		if (stream == null) return null;
		using var reader = new StreamReader(stream);
		return reader.ReadToEnd();
	}
}
