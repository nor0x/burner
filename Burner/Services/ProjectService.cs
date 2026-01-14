using Burner.Models;

namespace Burner.Services;

public class ProjectService
{
	private readonly BurnerConfig _config;

	public ProjectService(BurnerConfig config)
	{
		_config = config;
	}

	public IEnumerable<BurnerProject> GetAllProjects()
	{
		if (!Directory.Exists(_config.BurnerHome))
			return Enumerable.Empty<BurnerProject>();

		return Directory.GetDirectories(_config.BurnerHome)
			.Select(BurnerProject.FromDirectory)
			.Where(p => p != null)
			.Cast<BurnerProject>()
			.OrderByDescending(p => p.CreatedAt);
	}

	public BurnerProject? GetProject(string name)
	{
		var projects = GetAllProjects();
		return projects.FirstOrDefault(p =>
			p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
			p.Name.EndsWith($"-{name}", StringComparison.OrdinalIgnoreCase));
	}

	public int CleanupProjects(int? days = null)
	{
		var cleanupDays = days ?? _config.AutoCleanDays;
		if (cleanupDays <= 0) return 0;

		var projects = GetAllProjects()
			.Where(p => p.AgeInDays > cleanupDays)
			.ToList();

		foreach (var project in projects)
		{
			try
			{
				Directory.Delete(project.Path, recursive: true);
			}
			catch
			{
				// Skip projects that can't be deleted
			}
		}

		return projects.Count;
	}

	public bool DeleteProject(string name)
	{
		var project = GetProject(name);
		if (project == null) return false;

		try
		{
			Directory.Delete(project.Path, recursive: true);
			return true;
		}
		catch
		{
			return false;
		}
	}

	public (bool Success, string? Path) ImportProject(string sourceDir, string projectName, bool copy = false)
	{
		// Ensure burner home exists
		Directory.CreateDirectory(_config.BurnerHome);

		// Generate dated project name with YYMMDD prefix
		var datedProjectName = $"{DateTime.Now:yyMMdd}-{projectName}";
		var destinationPath = System.IO.Path.Combine(_config.BurnerHome, datedProjectName);

		// Check if destination already exists
		if (Directory.Exists(destinationPath))
		{
			return (false, null);
		}

		// Check if source directory exists and is valid
		if (!Directory.Exists(sourceDir))
		{
			return (false, null);
		}

		try
		{
			if (copy)
			{
				// Copy the directory
				CopyDirectory(sourceDir, destinationPath);
			}
			else
			{
				// Move the directory
				Directory.Move(sourceDir, destinationPath);
			}

			// Create a marker file to indicate this is a custom imported project
			File.WriteAllText(System.IO.Path.Combine(destinationPath, ".burner-custom"), "");

			return (true, destinationPath);
		}
		catch
		{
			return (false, null);
		}
	}

	private static void CopyDirectory(string sourceDir, string destinationDir)
	{
		// Create destination directory
		Directory.CreateDirectory(destinationDir);

		// Copy all files
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var fileName = System.IO.Path.GetFileName(file);
			var destFile = System.IO.Path.Combine(destinationDir, fileName);
			// Use overwrite: true since we're copying to a new directory we control
			File.Copy(file, destFile, overwrite: true);
		}

		// Recursively copy subdirectories
		foreach (var dir in Directory.GetDirectories(sourceDir))
		{
			var dirName = System.IO.Path.GetFileName(dir);
			var destDir = System.IO.Path.Combine(destinationDir, dirName);
			CopyDirectory(dir, destDir);
		}
	}
}
