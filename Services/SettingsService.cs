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
        RemoveMissingAccounts(settings);
        return settings;
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(BaseFolder);
        PathLayoutService.NormalizeSettings(settings);
        NormalizeAccounts(settings.Accounts);
        RemoveMissingAccounts(settings);

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

    private static void RemoveMissingAccounts(AppSettings settings)
    {
        if (settings.Accounts.Count == 0 || string.IsNullOrWhiteSpace(settings.GameInstallPath) || !Directory.Exists(settings.GameInstallPath))
        {
            return;
        }

        settings.Accounts = settings.Accounts
            .Where(account => AccountStillExists(settings.GameInstallPath, account))
            .OrderBy(account => account.SlotNumber)
            .ToList();
    }

    private static bool AccountStillExists(string gameInstallPath, AccountProfile account)
    {
        if (account.SlotNumber <= 0)
        {
            return false;
        }

        var code = string.IsNullOrWhiteSpace(account.Code) ? $"ro_win{account.SlotNumber}" : account.Code;
        var executablePath = Path.Combine(gameInstallPath, $"{code}.exe");
        var dataPath = Path.Combine(gameInstallPath, $"{code}_Data");

        return File.Exists(executablePath) || Directory.Exists(dataPath);
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
