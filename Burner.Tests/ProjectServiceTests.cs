using Burner.Models;
using Burner.Services;

namespace Burner.Tests;

public class ProjectServiceTests : IDisposable
{
	private readonly string _testDir;
	private readonly BurnerConfig _config;
	private readonly ProjectService _service;

	public ProjectServiceTests()
	{
		_testDir = Path.Combine(Path.GetTempPath(), $"burner-test-{Guid.NewGuid()}");
		Directory.CreateDirectory(_testDir);

		_config = new BurnerConfig
		{
			BurnerHome = _testDir,
			AutoCleanDays = 30
		};
		_service = new ProjectService(_config);
	}

	public void Dispose()
	{
		if (Directory.Exists(_testDir))
		{
			Directory.Delete(_testDir, recursive: true);
		}
	}

	[Fact]
	public void GetAllProjects_ReturnsEmpty_WhenNoProjects()
	{
		var projects = _service.GetAllProjects();

		Assert.Empty(projects);
	}

	[Fact]
	public void GetAllProjects_ReturnsProjects_WhenDirectoriesExist()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "260107-project1"));
		Directory.CreateDirectory(Path.Combine(_testDir, "260108-project2"));

		var projects = _service.GetAllProjects().ToList();

		Assert.Equal(2, projects.Count);
	}

	[Fact]
	public void GetAllProjects_OrdersByDateDescending()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "260105-older"));
		Directory.CreateDirectory(Path.Combine(_testDir, "260110-newer"));

		var projects = _service.GetAllProjects().ToList();

		Assert.Equal("260110-newer", projects[0].Name);
		Assert.Equal("260105-older", projects[1].Name);
	}

	[Fact]
	public void GetProject_FindsByExactName()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "260107-my-app"));

		var project = _service.GetProject("260107-my-app");

		Assert.NotNull(project);
		Assert.Equal("260107-my-app", project.Name);
	}

	[Fact]
	public void GetProject_FindsByPartialName()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "260107-my-app"));

		var project = _service.GetProject("my-app");

		Assert.NotNull(project);
		Assert.Equal("260107-my-app", project.Name);
	}

	[Fact]
	public void GetProject_ReturnsNull_WhenNotFound()
	{
		var project = _service.GetProject("nonexistent");

		Assert.Null(project);
	}

	[Fact]
	public void DeleteProject_RemovesDirectory()
	{
		var projectPath = Path.Combine(_testDir, "260107-to-delete");
		Directory.CreateDirectory(projectPath);
		File.WriteAllText(Path.Combine(projectPath, "file.txt"), "content");

		var result = _service.DeleteProject("to-delete");

		Assert.True(result);
		Assert.False(Directory.Exists(projectPath));
	}

	[Fact]
	public void DeleteProject_ReturnsFalse_WhenNotFound()
	{
		var result = _service.DeleteProject("nonexistent");

		Assert.False(result);
	}

	[Fact]
	public void CleanupProjects_RemovesOldProjects()
	{
		// Create a project with an old date in the name
		var oldProjectPath = Path.Combine(_testDir, "240101-very-old");
		Directory.CreateDirectory(oldProjectPath);

		// Create a recent project
		var newProjectPath = Path.Combine(_testDir, $"{DateTime.Now:yyMMdd}-new");
		Directory.CreateDirectory(newProjectPath);

		var deleted = _service.CleanupProjects(days: 30);

		Assert.True(deleted >= 1);
		Assert.False(Directory.Exists(oldProjectPath));
		Assert.True(Directory.Exists(newProjectPath));
	}

	[Fact]
	public void CleanupProjects_ReturnsZero_WhenDisabled()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "240101-old"));

		var deleted = _service.CleanupProjects(days: 0);

		Assert.Equal(0, deleted);
	}

	[Fact]
	public void DeleteAllProjects_RemovesAllProjects()
	{
		Directory.CreateDirectory(Path.Combine(_testDir, "260101-project1"));
		Directory.CreateDirectory(Path.Combine(_testDir, "260102-project2"));
		Directory.CreateDirectory(Path.Combine(_testDir, "260103-project3"));

		var projects = _service.GetAllProjects().ToList();
		Assert.Equal(3, projects.Count);

		var deleted = 0;
		foreach (var project in projects)
		{
			if (_service.DeleteProject(project.Name))
				deleted++;
		}

		Assert.Equal(3, deleted);
		Assert.Empty(_service.GetAllProjects());
	}

	[Fact]
	public void DeleteAllProjects_ReturnsZero_WhenNoProjects()
	{
		var projects = _service.GetAllProjects().ToList();
		Assert.Empty(projects);

		var deleted = 0;
		foreach (var project in projects)
		{
			if (_service.DeleteProject(project.Name))
				deleted++;
		}

		Assert.Equal(0, deleted);
	}

	[Fact]
	public void DeleteAllProjects_HandlesProjectsWithFiles()
	{
		var project1 = Path.Combine(_testDir, "260101-with-files");
		var project2 = Path.Combine(_testDir, "260102-with-nested");

		Directory.CreateDirectory(project1);
		File.WriteAllText(Path.Combine(project1, "file.txt"), "content");

		Directory.CreateDirectory(Path.Combine(project2, "subfolder"));
		File.WriteAllText(Path.Combine(project2, "subfolder", "nested.txt"), "nested");

		var projects = _service.GetAllProjects().ToList();
		foreach (var project in projects)
		{
			_service.DeleteProject(project.Name);
		}

		Assert.False(Directory.Exists(project1));
		Assert.False(Directory.Exists(project2));
		Assert.Empty(_service.GetAllProjects());
	}

	[Fact]
	public void ImportProject_MovesFolder_WithDatePrefix()
	{
		// Create a source directory to import
		var sourceDir = Path.Combine(Path.GetTempPath(), $"source-{Guid.NewGuid()}");
		Directory.CreateDirectory(sourceDir);
		File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "content");

		// Get the destination path first (as the command now does)
		var destinationPath = _service.GetImportDestinationPath("my-import");
		Assert.NotNull(destinationPath);
		Directory.CreateDirectory(destinationPath);

		var result = _service.ImportProject(sourceDir, destinationPath, copy: false);

		Assert.True(result.Success);
		Assert.NotNull(result.Path);
		Assert.True(Directory.Exists(sourceDir)); // Source folder still exists but is empty
		Assert.Empty(Directory.GetFiles(sourceDir));
		Assert.Empty(Directory.GetDirectories(sourceDir));
		Assert.True(Directory.Exists(result.Path)); // Destination should exist
		Assert.Contains($"{DateTime.Now:yyMMdd}-my-import", result.Path);
		Assert.True(File.Exists(Path.Combine(result.Path, "test.txt")));
		Assert.True(File.Exists(Path.Combine(result.Path, ".burner-custom")));

		// Cleanup
		Directory.Delete(sourceDir, true);
		if (result.Path != null && Directory.Exists(result.Path))
			Directory.Delete(result.Path, true);
	}

	[Fact]
	public void ImportProject_CopiesFolder_WhenCopyFlagSet()
	{
		// Create a source directory to import
		var sourceDir = Path.Combine(Path.GetTempPath(), $"source-{Guid.NewGuid()}");
		Directory.CreateDirectory(sourceDir);
		File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "content");

		// Get the destination path first (as the command now does)
		var destinationPath = _service.GetImportDestinationPath("my-copy");
		Assert.NotNull(destinationPath);
		Directory.CreateDirectory(destinationPath);

		var result = _service.ImportProject(sourceDir, destinationPath, copy: true);

		Assert.True(result.Success);
		Assert.NotNull(result.Path);
		Assert.True(Directory.Exists(sourceDir)); // Source should still exist
		Assert.True(Directory.Exists(result.Path)); // Destination should exist
		Assert.Contains($"{DateTime.Now:yyMMdd}-my-copy", result.Path);
		Assert.True(File.Exists(Path.Combine(result.Path, "test.txt")));
		Assert.True(File.Exists(Path.Combine(result.Path, ".burner-custom")));

		// Cleanup
		Directory.Delete(sourceDir, true);
		if (result.Path != null && Directory.Exists(result.Path))
			Directory.Delete(result.Path, true);
	}

	[Fact]
	public void ImportProject_CopiesNestedFolders()
	{
		// Create a source directory with nested structure
		var sourceDir = Path.Combine(Path.GetTempPath(), $"source-{Guid.NewGuid()}");
		Directory.CreateDirectory(Path.Combine(sourceDir, "subfolder", "nested"));
		File.WriteAllText(Path.Combine(sourceDir, "root.txt"), "root");
		File.WriteAllText(Path.Combine(sourceDir, "subfolder", "sub.txt"), "sub");
		File.WriteAllText(Path.Combine(sourceDir, "subfolder", "nested", "deep.txt"), "deep");

		// Get the destination path first (as the command now does)
		var destinationPath = _service.GetImportDestinationPath("nested-test");
		Assert.NotNull(destinationPath);
		Directory.CreateDirectory(destinationPath);

		var result = _service.ImportProject(sourceDir, destinationPath, copy: true);

		Assert.True(result.Success);
		Assert.NotNull(result.Path);
		Assert.True(File.Exists(Path.Combine(result.Path, "root.txt")));
		Assert.True(File.Exists(Path.Combine(result.Path, "subfolder", "sub.txt")));
		Assert.True(File.Exists(Path.Combine(result.Path, "subfolder", "nested", "deep.txt")));

		// Cleanup
		Directory.Delete(sourceDir, true);
		if (result.Path != null && Directory.Exists(result.Path))
			Directory.Delete(result.Path, true);
	}

	[Fact]
	public void ImportProject_ReturnsFalse_WhenSourceDoesNotExist()
	{
		var destinationPath = _service.GetImportDestinationPath("test");
		Assert.NotNull(destinationPath);

		var result = _service.ImportProject("/nonexistent/path", destinationPath, copy: false);

		Assert.False(result.Success);
		Assert.Null(result.Path);
	}

	[Fact]
	public void GetImportDestinationPath_ReturnsNull_WhenDestinationExists()
	{
		// Create a destination that already exists with the same dated name
		var datedName = $"{DateTime.Now:yyMMdd}-duplicate";
		var existingDest = Path.Combine(_testDir, datedName);
		Directory.CreateDirectory(existingDest);

		var destinationPath = _service.GetImportDestinationPath("duplicate");

		Assert.Null(destinationPath);
	}
}
