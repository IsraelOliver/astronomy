using System;
using System.IO;
using System.Text.Json;

namespace Astronomia;

public sealed class UserSettings
{
    private const string FileName = "settings.json";

    public Language Language { get; set; } = Language.English;

    public static UserSettings Load()
    {
        var path = GetSettingsPath();

        if (!File.Exists(path))
            return new UserSettings();

        try
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<UserSettings>(json) ?? new UserSettings();
        }
        catch
        {
            return new UserSettings();
        }
    }

    public void Save()
    {
        var path = GetSettingsPath();
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(path, json);
    }

    private static string GetSettingsPath()
    {
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "Astronomia");

        Directory.CreateDirectory(directory);
        return Path.Combine(directory, FileName);
    }
}
