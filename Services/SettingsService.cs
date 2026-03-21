using System.IO;
using System.Text.Json;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class SettingsService
{
    private static readonly string BaseFolder = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "RoLauncher");

    private static readonly string SettingsFile = Path.Combine(BaseFolder, "settings.json");

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public string SettingsFilePath => SettingsFile;

    public AppSettings Load()
    {
        Directory.CreateDirectory(BaseFolder);

        if (!File.Exists(SettingsFile))
        {
            return CreateDefaultSettings();
        }

        var json = File.ReadAllText(SettingsFile);
        var persisted = JsonSerializer.Deserialize<PersistedSettings>(json, _options) ?? new PersistedSettings();

        var settings = new AppSettings
        {
            GameInstallPath = persisted.GameInstallPath,
            AppDataBasePath = persisted.AppDataBasePath,
            AppDataPcPath = string.IsNullOrWhiteSpace(persisted.AppDataPcPath)
                ? persisted.RuntimeTokenPath
                : persisted.AppDataPcPath,
            InstancesRootPath = persisted.InstancesRootPath,
            TokenRules = persisted.TokenRules ?? new List<TokenRule>(),
            Accounts = persisted.Accounts ?? new List<AccountProfile>()
        };

        PathLayoutService.NormalizeSettings(settings);
        NormalizeAccounts(settings.Accounts);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(BaseFolder);
        PathLayoutService.NormalizeSettings(settings);
        NormalizeAccounts(settings.Accounts);

        var json = JsonSerializer.Serialize(settings, _options);
        File.WriteAllText(SettingsFile, json);
    }

    private static AppSettings CreateDefaultSettings()
    {
        return new AppSettings();
    }

    private static void NormalizeAccounts(List<AccountProfile> accounts)
    {
        foreach (var account in accounts)
        {
            account.Code ??= string.Empty;
            account.DisplayName = string.IsNullOrWhiteSpace(account.DisplayName)
                ? account.Code
                : account.DisplayName.Trim();
            account.InstanceFolderPath ??= string.Empty;
            account.ExecutablePath ??= string.Empty;
            account.ShortcutPath ??= string.Empty;
            account.BackupTokenFolderPath ??= string.Empty;
        }
    }

    private sealed class PersistedSettings
    {
        public string GameInstallPath { get; set; } = string.Empty;
        public string AppDataBasePath { get; set; } = string.Empty;
        public string AppDataPcPath { get; set; } = string.Empty;
        public string InstancesRootPath { get; set; } = string.Empty;
        public string RuntimeTokenPath { get; set; } = string.Empty;
        public List<TokenRule>? TokenRules { get; set; }
        public List<AccountProfile>? Accounts { get; set; }
    }
}
