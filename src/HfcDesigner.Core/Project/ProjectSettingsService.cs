using System.Text.Json;

namespace HfcDesigner.Core.Project;

/// <summary>Loads/saves .hdproj project settings files and tracks a small recent-projects list.</summary>
public sealed class ProjectSettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public const string ProjectFileExtension = ".hdproj";

    public static string AppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "HfcDesigner");

    public static string RecentProjectsPath => Path.Combine(AppDataFolder, "recent-projects.json");

    public void Save(ProjectSettings settings, string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(filePath, json);
        AddToRecent(filePath);
    }

    public ProjectSettings? Load(string filePath)
    {
        if (!File.Exists(filePath)) return null;
        var json = File.ReadAllText(filePath);
        return JsonSerializer.Deserialize<ProjectSettings>(json);
    }

    public List<string> LoadRecentProjects()
    {
        if (!File.Exists(RecentProjectsPath)) return [];
        try
        {
            var json = File.ReadAllText(RecentProjectsPath);
            return JsonSerializer.Deserialize<List<string>>(json) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private void AddToRecent(string filePath)
    {
        var recents = LoadRecentProjects();
        recents.RemoveAll(p => string.Equals(p, filePath, StringComparison.OrdinalIgnoreCase));
        recents.Insert(0, filePath);
        if (recents.Count > 10) recents = recents.Take(10).ToList();

        Directory.CreateDirectory(AppDataFolder);
        File.WriteAllText(RecentProjectsPath, JsonSerializer.Serialize(recents, JsonOptions));
    }
}
