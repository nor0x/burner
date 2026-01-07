using Burner.Models;

namespace Burner.Tests;

public class BurnerConfigTests : IDisposable
{
	private readonly string _testConfigDir;
	private readonly string _originalUserProfile;

	public BurnerConfigTests()
	{
		_testConfigDir = Path.Combine(Path.GetTempPath(), $"burner-test-{Guid.NewGuid()}");
		Directory.CreateDirectory(_testConfigDir);

		_originalUserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testConfigDir))
		{
			Directory.Delete(_testConfigDir, recursive: true);
		}
	}

	[Fact]
	public void DefaultConfig_HasCorrectDefaults()
	{
		var config = new BurnerConfig();

		Assert.Equal(30, config.AutoCleanDays);
		Assert.Contains(".burner", config.BurnerHome);
		Assert.Contains("projects", config.BurnerHome);
		Assert.Contains(".burner", config.BurnerTemplates);
		Assert.Contains("templates", config.BurnerTemplates);
	}

	[Fact]
	public void AutoCleanDays_CanBeSetToZero()
	{
		var config = new BurnerConfig { AutoCleanDays = 0 };

		Assert.Equal(0, config.AutoCleanDays);
	}

	[Fact]
	public void BurnerHome_CanBeCustomized()
	{
		var customPath = @"C:\CustomBurner";
		var config = new BurnerConfig { BurnerHome = customPath };

		Assert.Equal(customPath, config.BurnerHome);
	}

	[Fact]
	public void GetConfigPath_ReturnsPathInBurnerDirectory()
	{
		var configPath = BurnerConfig.GetConfigPath();

		Assert.Contains(".burner", configPath);
		Assert.EndsWith("config.json", configPath);
	}
}
