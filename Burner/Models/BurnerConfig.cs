using System.Text.Json;
using System.Text.Json.Serialization;

namespace Burner.Models;

/// <summary>
/// Configuration settings for Burner CLI. Persisted to ~/.burner/config.json.
/// </summary>
public class BurnerConfig
{
	private static readonly string ConfigDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner");

	private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

	/// <summary>
	/// Gets or sets the directory where burner projects are stored.
	/// Default: ~/.burner/projects
	/// </summary>
	[JsonPropertyName("burnerHome")]
	public string BurnerHome { get; set; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner", "projects");

	/// <summary>
	/// Gets or sets the directory where project templates are stored.
	/// Default: ~/.burner/templates
	/// </summary>
	public string BurnerTemplates { get; set; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner", "templates");

	/// <summary>
	/// Gets or sets the number of days after which projects are eligible for auto-cleanup.
	/// Set to 0 to disable auto-cleanup. Default: 30
	/// </summary>
	[JsonPropertyName("autoCleanDays")]
	public int AutoCleanDays { get; set; } = 30;

	/// <summary>
	/// Gets or sets the editor command to use when opening projects (e.g., "code", "cursor", "rider").
	/// Default: "code" (VS Code)
	/// </summary>
	[JsonPropertyName("editor")]
	public string Editor { get; set; } = "code";

	/// <summary>
	/// Loads the configuration from disk, or creates a default configuration if none exists.
	/// </summary>
	/// <returns>The loaded or default configuration.</returns>
	public static BurnerConfig Load()
	{
		if (!File.Exists(ConfigPath))
		{
			var config = new BurnerConfig();
			config.Save();
			return config;
		}

		try
		{
			var json = File.ReadAllText(ConfigPath);
			return JsonSerializer.Deserialize<BurnerConfig>(json) ?? new BurnerConfig();
		}
		catch
		{
			return new BurnerConfig();
		}
	}

	/// <summary>
	/// Saves the current configuration to disk.
	/// </summary>
	public void Save()
	{
		Directory.CreateDirectory(ConfigDirectory);
		var options = new JsonSerializerOptions { WriteIndented = true };
		var json = JsonSerializer.Serialize(this, options);
		File.WriteAllText(ConfigPath, json);
	}

	/// <summary>
	/// Gets the path to the configuration file.
	/// </summary>
	/// <returns>The full path to the config.json file.</returns>
	public static string GetConfigPath() => ConfigPath;
}
