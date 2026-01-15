namespace Burner.Models;

/// <summary>
/// Represents a burner project with its metadata.
/// </summary>
public class BurnerProject
{
	/// <summary>
	/// Gets or sets the project name (includes date prefix in YYMMDD-name format).
	/// </summary>
	public string Name { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the full file system path to the project directory.
	/// </summary>
	public string Path { get; set; } = string.Empty;

	/// <summary>
	/// Gets or sets the date and time when the project was created.
	/// </summary>
	public DateTime CreatedAt { get; set; }

	/// <summary>
	/// Gets or sets the template used to create this project (e.g., "dotnet", "web", "custom").
	/// </summary>
	public string Template { get; set; } = string.Empty;

	/// <summary>
	/// Gets the age of the project in days since creation.
	/// </summary>
	public int AgeInDays => (DateTime.Now - CreatedAt).Days;

	/// <summary>
	/// Creates a BurnerProject instance from a directory path.
	/// </summary>
	/// <param name="path">The directory path to parse.</param>
	/// <returns>A BurnerProject instance, or null if the directory doesn't exist.</returns>
	public static BurnerProject? FromDirectory(string path)
	{
		var dirInfo = new DirectoryInfo(path);
		if (!dirInfo.Exists) return null;

		var name = dirInfo.Name;
		DateTime createdAt;

		// Try to parse date from YYMMDD-NAME format
		if (name.Length >= 6 && name.Contains('-'))
		{
			var datePart = name[..6];
			if (DateTime.TryParseExact(datePart, "yyMMdd", null,
				System.Globalization.DateTimeStyles.None, out var parsed))
			{
				createdAt = parsed;
			}
			else
			{
				createdAt = dirInfo.CreationTime;
			}
		}
		else
		{
			createdAt = dirInfo.CreationTime;
		}

		// Try to detect template from project contents
		var template = DetectTemplate(path);

		return new BurnerProject
		{
			Name = name,
			Path = path,
			CreatedAt = createdAt,
			Template = template
		};
	}

	private static string DetectTemplate(string projectPath)
	{
		// Check if this is a custom imported project
		if (File.Exists(System.IO.Path.Combine(projectPath, ".burner-custom")))
			return "custom";
		
		if (Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
			return "dotnet";
		if (File.Exists(System.IO.Path.Combine(projectPath, "index.html")))
			return "web";
		return "unknown";
	}
}
