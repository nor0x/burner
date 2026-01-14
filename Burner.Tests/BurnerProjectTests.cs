using Burner.Models;

namespace Burner.Tests;

public class BurnerProjectTests : IDisposable
{
	private readonly string _testDir;

	public BurnerProjectTests()
	{
		_testDir = Path.Combine(Path.GetTempPath(), $"burner-test-{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDir);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDir))
		{
			Directory.Delete(_testDir, recursive: true);
		}
	}

	[Fact]
	public void FromDirectory_ReturnsNull_WhenDirectoryDoesNotExist()
	{
		var result = BurnerProject.FromDirectory(@"C:\NonExistent\Path");

		Assert.Null(result);
	}

	[Fact]
	public void FromDirectory_ParsesDateFromName_WhenInCorrectFormat()
	{
		var projectPath = Path.Combine(_testDir, "260115-test-project");
		Directory.CreateDirectory(projectPath);

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("260115-test-project", project.Name);
		Assert.Equal(2026, project.CreatedAt.Year);
		Assert.Equal(1, project.CreatedAt.Month);
		Assert.Equal(15, project.CreatedAt.Day);
	}

	[Fact]
	public void FromDirectory_UsesCreationTime_WhenNameHasNoDate()
	{
		var projectPath = Path.Combine(_testDir, "my-project");
		Directory.CreateDirectory(projectPath);

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("my-project", project.Name);
		// Should use directory creation time
		Assert.True((DateTime.Now - project.CreatedAt).TotalMinutes < 1);
	}

	[Fact]
	public void FromDirectory_DetectsDotnetTemplate_WhenCsprojExists()
	{
		var projectPath = Path.Combine(_testDir, "260107-dotnet-app");
		Directory.CreateDirectory(projectPath);
		File.WriteAllText(Path.Combine(projectPath, "test.csproj"), "<Project></Project>");

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("dotnet", project.Template);
	}

	[Fact]
	public void FromDirectory_DetectsWebTemplate_WhenIndexHtmlExists()
	{
		var projectPath = Path.Combine(_testDir, "260107-web-app");
		Directory.CreateDirectory(projectPath);
		File.WriteAllText(Path.Combine(projectPath, "index.html"), "<html></html>");

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("web", project.Template);
	}

	[Fact]
	public void FromDirectory_ReturnsUnknown_WhenNoTemplateDetected()
	{
		var projectPath = Path.Combine(_testDir, "260107-mystery");
		Directory.CreateDirectory(projectPath);

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("unknown", project.Template);
	}

	[Fact]
	public void AgeInDays_CalculatesCorrectly()
	{
		var projectPath = Path.Combine(_testDir, "260107-test");
		Directory.CreateDirectory(projectPath);

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		// Project dated Jan 7, 2026 - age depends on current date
		var expectedAge = (DateTime.Now.Date - new DateTime(2026, 1, 7)).Days;
		Assert.Equal(expectedAge, project.AgeInDays);
	}

	[Fact]
	public void FromDirectory_DetectsCustomTemplate_WhenMarkerFileExists()
	{
		var projectPath = Path.Combine(_testDir, "260114-custom-import");
		Directory.CreateDirectory(projectPath);
		File.WriteAllText(Path.Combine(projectPath, ".burner-custom"), "");
		File.WriteAllText(Path.Combine(projectPath, "test.txt"), "content");

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("custom", project.Template);
	}

	[Fact]
	public void FromDirectory_PrioritizesCustomTemplate_OverOtherDetections()
	{
		// Even if there's a .csproj file, if .burner-custom exists, it should be "custom"
		var projectPath = Path.Combine(_testDir, "260114-custom-dotnet");
		Directory.CreateDirectory(projectPath);
		File.WriteAllText(Path.Combine(projectPath, ".burner-custom"), "");
		File.WriteAllText(Path.Combine(projectPath, "test.csproj"), "<Project></Project>");

		var project = BurnerProject.FromDirectory(projectPath);

		Assert.NotNull(project);
		Assert.Equal("custom", project.Template);
	}
}
