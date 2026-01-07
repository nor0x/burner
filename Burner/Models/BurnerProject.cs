namespace Burner.Models;

public class BurnerProject
{
	public string Name { get; set; } = string.Empty;
	public string Path { get; set; } = string.Empty;
	public DateTime CreatedAt { get; set; }
	public string Template { get; set; } = string.Empty;

	public int AgeInDays => (DateTime.Now - CreatedAt).Days;

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
		if (Directory.GetFiles(projectPath, "*.csproj", SearchOption.TopDirectoryOnly).Length > 0)
			return "dotnet";
		if (File.Exists(System.IO.Path.Combine(projectPath, "index.html")))
			return "web";
		return "unknown";
	}
}
