using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public static class PathLayoutService
{
    public static void NormalizeSettings(AppSettings settings)
    {
        settings.GameInstallPath = Normalize(settings.GameInstallPath);
        settings.AppDataBasePath = Normalize(settings.AppDataBasePath);
        settings.AppDataPcPath = Normalize(settings.AppDataPcPath);

        if (string.IsNullOrWhiteSpace(settings.AppDataPcPath) && !string.IsNullOrWhiteSpace(settings.AppDataBasePath))
        {
            settings.AppDataPcPath = Path.Combine(settings.AppDataBasePath, "XD", "PC");
        }

        if (string.IsNullOrWhiteSpace(settings.AppDataBasePath) && !string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            settings.AppDataBasePath = TryDeriveBasePath(settings.AppDataPcPath);
        }
    }

    public static string ResolveRuntimeDataPath(AppSettings settings)
    {
        NormalizeSettings(settings);

        if (!string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            return settings.AppDataPcPath;
        }

        throw new InvalidOperationException("Informe a pasta AppData LocalLow\\XD\\PC.");
    }

    public static string ResolveInstancesRoot(AppSettings settings)
    {
        NormalizeSettings(settings);

        if (!string.IsNullOrWhiteSpace(settings.InstancesRootPath))
        {
            return settings.InstancesRootPath;
        }

        throw new InvalidOperationException("Informe a pasta raiz das instâncias clonadas.");
    }

    public static string TryDeriveBasePath(string appDataPcPath)
    {
        var normalized = Normalize(appDataPcPath);
        var current = new DirectoryInfo(normalized);

        if (current.Name.Equals("PC", StringComparison.OrdinalIgnoreCase) &&
            current.Parent?.Name.Equals("XD", StringComparison.OrdinalIgnoreCase) == true &&
            current.Parent.Parent is not null)
        {
            return current.Parent.Parent.FullName;
        }

        return current.Parent?.FullName ?? normalized;
    }

    private static string Normalize(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        return Path.GetFullPath(path.Trim());
    }
}
