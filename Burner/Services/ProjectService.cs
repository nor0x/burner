using Burner.Models;

namespace Burner.Services;

/// <summary>
/// Manages burner projects - listing, retrieving, deleting, and importing.
/// </summary>
public class ProjectService
{
	private readonly BurnerConfig _config;

	/// <summary>
	/// Creates a new instance of the ProjectService.
	/// </summary>
	/// <param name="config">The burner configuration to use.</param>
	public ProjectService(BurnerConfig config)
	{
		_config = config;
	}

	/// <summary>
	/// Returns all projects in the burner home directory, sorted by creation date (newest first).
	/// </summary>
	/// <returns>An enumerable of all burner projects.</returns>
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

	/// <summary>
	/// Gets a project by name. Supports partial name matching (suffix match).
	/// </summary>
	/// <param name="name">The project name or partial name to search for.</param>
	/// <returns>The matching project, or null if not found.</returns>
	public BurnerProject? GetProject(string name)
	{
		var projects = GetAllProjects();
		return projects.FirstOrDefault(p =>
			p.Name.Equals(name, StringComparison.OrdinalIgnoreCase) ||
			p.Name.EndsWith($"-{name}", StringComparison.OrdinalIgnoreCase));
	}

	/// <summary>
	/// Deletes projects older than the specified days. Uses config default if not provided.
	/// </summary>
	/// <param name="days">Number of days threshold. Projects older than this will be deleted.</param>
	/// <returns>Count of deleted projects.</returns>
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

	/// <summary>
	/// Deletes a project by name.
	/// </summary>
	/// <param name="name">The project name to delete.</param>
	/// <returns>True if successful, false otherwise.</returns>
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

	/// <summary>
	/// Gets the destination path for an import operation without performing the import.
	/// </summary>
	/// <param name="projectName">The name for the imported project.</param>
	/// <returns>The destination path, or null if destination already exists.</returns>
	public string? GetImportDestinationPath(string projectName)
	{
		// Ensure burner home exists
		Directory.CreateDirectory(_config.BurnerHome);

		// Generate dated project name with YYMMDD prefix
		var datedProjectName = $"{DateTime.Now:yyMMdd}-{projectName}";
		var destinationPath = System.IO.Path.Combine(_config.BurnerHome, datedProjectName);

		// Check if destination already exists
		if (Directory.Exists(destinationPath))
		{
			return null;
		}

		return destinationPath;
	}

	/// <summary>
	/// Imports a directory to burner home. Moves by default, copies if specified.
	/// </summary>
	/// <param name="sourceDir">The source directory to import.</param>
	/// <param name="destinationPath">The destination path for the import.</param>
	/// <param name="copy">If true, copies the directory; if false, moves it.</param>
	/// <returns>A tuple containing success status and the resulting path.</returns>
	public (bool Success, string? Path) ImportProject(string sourceDir, string destinationPath, bool copy = false)
	{
		// Check if source directory exists and is valid
		if (!Directory.Exists(sourceDir))
		{
			return (false, null);
		}

		try
		{
			if (copy)
			{
				// Copy directory contents to destination
				CopyDirectory(sourceDir, destinationPath);
			}
			else
			{
				// Move contents from source to destination (which should already exist)
				MoveDirectoryContents(sourceDir, destinationPath);
			}

			// Create a marker file to indicate this is a custom imported project
			File.WriteAllText(System.IO.Path.Combine(destinationPath, ".burner-custom"), "");

			return (true, destinationPath);
		}
		catch (IOException ex)
		{
			// I/O-related failure during import (e.g., file in use, disk error)
			System.Diagnostics.Debug.WriteLine($"Import failed with IOException: {ex.Message}");
			return (false, null);
		}
		catch (UnauthorizedAccessException ex)
		{
			// Permission-related failure during import
			System.Diagnostics.Debug.WriteLine($"Import failed with UnauthorizedAccessException: {ex.Message}");
			return (false, null);
		}
		catch (Exception ex)
		{
			// Catch any other unexpected exceptions
			System.Diagnostics.Debug.WriteLine($"Import failed with unexpected exception: {ex.Message}");
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
			
			// Check if the file is a symbolic link
			var fileInfo = new FileInfo(file);
			if (fileInfo.LinkTarget != null)
			{
				// Preserve symbolic link instead of following it
				File.CreateSymbolicLink(destFile, fileInfo.LinkTarget);
			}
			else
			{
				// Use overwrite: true since we're copying to a new directory we control
				File.Copy(file, destFile, overwrite: true);
			}
		}

		// Recursively copy subdirectories
		foreach (var dir in Directory.GetDirectories(sourceDir))
		{
			var dirInfo = new DirectoryInfo(dir);
			var dirName = System.IO.Path.GetFileName(dir);
			var destDir = System.IO.Path.Combine(destinationDir, dirName);
			
			// Check if the directory is a symbolic link
			if (dirInfo.LinkTarget != null)
			{
				// Preserve directory symbolic link instead of following it
				Directory.CreateSymbolicLink(destDir, dirInfo.LinkTarget);
			}
			else
			{
				// Recursively copy directory contents
				CopyDirectory(dir, destDir);
			}
		}
	}

	private static void MoveDirectoryContents(string sourceDir, string destinationDir)
	{
		// Ensure destination exists
		Directory.CreateDirectory(destinationDir);

		// Move all files
		foreach (var file in Directory.GetFiles(sourceDir))
		{
			var fileName = System.IO.Path.GetFileName(file);
			var destFile = System.IO.Path.Combine(destinationDir, fileName);

			// Avoid ambiguous IOException by explicitly checking for existing files
			if (File.Exists(destFile))
			{
				throw new IOException(
					$"Cannot move file '{file}' to '{destFile}' because a file with the same name already exists in the destination directory.");
			}
			File.Move(file, destFile);
		}

		// Move all subdirectories
		foreach (var dir in Directory.GetDirectories(sourceDir))
		{
			var dirName = System.IO.Path.GetFileName(dir);
			var destDir = System.IO.Path.Combine(destinationDir, dirName);
			Directory.Move(dir, destDir);
		}

		// Note: We intentionally don't delete the empty source directory here
		// because the parent shell may still have it as its working directory.
		// Deleting it would cause "Unable to find current directory" errors.
	}
}
