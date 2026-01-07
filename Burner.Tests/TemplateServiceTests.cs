using Burner.Models;
using Burner.Services;

namespace Burner.Tests;

public class TemplateServiceTests : IDisposable
{
	private readonly string _testDir;
	private readonly string _templatesDir;
	private readonly BurnerConfig _config;
	private readonly TemplateService _service;

	public TemplateServiceTests()
	{
		_testDir = Path.Combine(Path.GetTempPath(), $"burner-test-{Guid.NewGuid()}");
		_templatesDir = Path.Combine(_testDir, "templates");
		Directory.CreateDirectory(_testDir);
		Directory.CreateDirectory(_templatesDir);

		_config = new BurnerConfig
		{
			BurnerHome = _testDir,
			BurnerTemplates = _templatesDir
		};
		_service = new TemplateService(_config);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDir))
		{
			Directory.Delete(_testDir, recursive: true);
		}
	}

	[Fact]
	public void GetAvailableTemplates_IncludesBuiltInTemplates()
	{
		var templates = _service.GetAvailableTemplates().ToList();

		Assert.Contains("dotnet", templates);
		Assert.Contains("web", templates);
	}

	[Fact]
	public void GetAvailableTemplates_IncludesCustomTemplates()
	{
		File.WriteAllText(Path.Combine(_templatesDir, "react.ps1"), "# React template");
		File.WriteAllText(Path.Combine(_templatesDir, "vue.bat"), "@echo Vue template");

		var templates = _service.GetAvailableTemplates().ToList();

		Assert.Contains("react", templates);
		Assert.Contains("vue", templates);
	}

	[Fact]
	public void GetAvailableTemplates_IgnoresNonExecutableFiles()
	{
		File.WriteAllText(Path.Combine(_templatesDir, "readme.txt"), "Not a template");
		File.WriteAllText(Path.Combine(_templatesDir, "notes.md"), "# Notes");

		var templates = _service.GetAvailableTemplates().ToList();

		Assert.DoesNotContain("readme", templates);
		Assert.DoesNotContain("notes", templates);
	}

	[Fact]
	public void CreateProject_CreatesDirectoryWithDatePrefix()
	{
		var result = _service.CreateProject("web", "my-test");

		Assert.True(result);

		var expectedPrefix = DateTime.Now.ToString("yyMMdd");
		var projectDirs = Directory.GetDirectories(_testDir, $"{expectedPrefix}-my-test");
		Assert.Single(projectDirs);
	}

	[Fact]
	public void CreateProject_WebTemplate_CreatesRequiredFiles()
	{
		_service.CreateProject("web", "web-test");

		var projectDir = Directory.GetDirectories(_testDir, "*-web-test").First();

		Assert.True(File.Exists(Path.Combine(projectDir, "index.html")));
		Assert.True(File.Exists(Path.Combine(projectDir, "styles.css")));
		Assert.True(File.Exists(Path.Combine(projectDir, "script.js")));
	}

	[Fact]
	public void CreateProject_ReturnsFalse_WhenProjectExists()
	{
		var existingPath = Path.Combine(_testDir, $"{DateTime.Now:yyMMdd}-duplicate");
		Directory.CreateDirectory(existingPath);

		var result = _service.CreateProject("web", "duplicate");

		Assert.False(result);
	}

	[Fact]
	public void CreateProject_ReturnsFalse_ForUnknownTemplate()
	{
		var result = _service.CreateProject("unknown-template", "test");

		Assert.False(result);
	}
}
