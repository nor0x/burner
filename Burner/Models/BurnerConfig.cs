using System.Text.Json;
using System.Text.Json.Serialization;

namespace Burner.Models;

public class BurnerConfig
{
	private static readonly string ConfigDirectory = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner");

	private static readonly string ConfigPath = Path.Combine(ConfigDirectory, "config.json");

	[JsonPropertyName("burnerHome")]
	public string BurnerHome { get; set; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner", "projects");
	public string BurnerTemplates { get; set; } = Path.Combine(
		Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".burner", "templates");

	[JsonPropertyName("autoCleanDays")]
	public int AutoCleanDays { get; set; } = 30;

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

	public void Save()
	{
		Directory.CreateDirectory(ConfigDirectory);
		var options = new JsonSerializerOptions { WriteIndented = true };
		var json = JsonSerializer.Serialize(this, options);
		File.WriteAllText(ConfigPath, json);
	}

	public static string GetConfigPath() => ConfigPath;
}
