using RoLauncher.Models;
using System.IO;
using System.Text.Json;

namespace RoLauncher.Services;

public sealed class SettingsService
{
    private static readonly string BaseFolder =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RoLauncher");

    private static readonly string SettingsFile =
        Path.Combine(BaseFolder, "settings.json");

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public AppSettings Load()
    {
        Directory.CreateDirectory(BaseFolder);

        if (!File.Exists(SettingsFile))
            return new AppSettings();

        string json = File.ReadAllText(SettingsFile);
        return JsonSerializer.Deserialize<AppSettings>(json, _options) ?? new AppSettings();
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(BaseFolder);
        string json = JsonSerializer.Serialize(settings, _options);
        File.WriteAllText(SettingsFile, json);
    }
}