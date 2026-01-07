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
}
