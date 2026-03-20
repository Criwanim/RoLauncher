using RoLauncher.Models;
using System.IO;
using System.Text.Json;

namespace RoLauncher.Services;

public sealed class SettingsService
{
    private static readonly string BaseFolder =
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoLauncher");

    private static readonly string SettingsFile =
        Path.Combine(BaseFolder, "settings.json");

    private readonly JsonSerializerOptions _options = new()
    {
        WriteIndented = true
    };

    public AppSettings Load()
    {
        Directory.CreateDirectory(BaseFolder);

        AppSettings settings;

        if (!File.Exists(SettingsFile))
        {
            settings = new AppSettings();
        }
        else
        {
            string json = File.ReadAllText(SettingsFile);
            settings = JsonSerializer.Deserialize<AppSettings>(json, _options)
                       ?? new AppSettings();
        }

        bool changed = false;

        // 🔹 Detectar AppData LocalLow
        if (string.IsNullOrWhiteSpace(settings.AppDataBasePath) ||
            string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            var appData = DetectAppDataPaths();

            if (appData != null)
            {
                settings.AppDataBasePath = appData.Value.basePath;
                settings.AppDataPcPath = appData.Value.pcPath;
                changed = true;
            }
        }

        // 🔹 Detectar instalação do jogo
        if (string.IsNullOrWhiteSpace(settings.GameInstallPath))
        {
            var gamePath = DetectGameInstallPath();

            if (!string.IsNullOrWhiteSpace(gamePath))
            {
                settings.GameInstallPath = gamePath;
                changed = true;
            }
        }

        // Salvar automaticamente se algo foi detectado
        if (changed)
            Save(settings);

        return settings;
    }

    public void Save(AppSettings settings)
    {
        Directory.CreateDirectory(BaseFolder);

        string json = JsonSerializer.Serialize(settings, _options);
        File.WriteAllText(SettingsFile, json);
    }

    // =========================================================
    // 🔍 DETECÇÃO DO APPDATA LOCALLOW
    // =========================================================

    private static (string basePath, string pcPath)? DetectAppDataPaths()
    {
        string local = Environment.GetFolderPath(
            Environment.SpecialFolder.LocalApplicationData);

        // C:\Users\<user>\AppData
        string appDataRoot = Directory.GetParent(local)!.FullName;

        string localLow = Path.Combine(appDataRoot, "LocalLow");

        string basePath = Path.Combine(
            localLow,
            "X_D_ Network Inc_",
            "Ragnarok M_ Classic Global");

        string pcPath = Path.Combine(basePath, "XD", "PC");

        if (Directory.Exists(basePath) && Directory.Exists(pcPath))
            return (basePath, pcPath);

        return null;
    }

    // =========================================================
    // 🎮 DETECÇÃO DO JOGO EM TODOS OS DISCOS
    // =========================================================

    private static string? DetectGameInstallPath()
    {
        foreach (var drive in DriveInfo.GetDrives())
        {
            try
            {
                // Ignora drives não prontos (CD, USB vazio, etc)
                if (!drive.IsReady)
                    continue;

                string path = Path.Combine(
                    drive.RootDirectory.FullName,
                    "Program Files (x86)",
                    "XD",
                    "Ragnarok M Classic Global");

                if (Directory.Exists(path))
                    return path;
            }
            catch
            {
                // Ignora erros de acesso
            }
        }

        return null;
    }
}