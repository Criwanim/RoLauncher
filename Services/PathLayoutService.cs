using System;
using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public static class PathLayoutService
{
    private const string VendorFolderName = "X_D_Network Inc_";
    private const string GameFolderName = "Ragnarok M_Classic Global";

    public static void NormalizeSettings(AppSettings settings)
    {
        settings.GameInstallPath = Normalize(settings.GameInstallPath);
        settings.AppDataBasePath = Normalize(settings.AppDataBasePath);
        settings.AppDataPcPath = Normalize(settings.AppDataPcPath);

        if (string.IsNullOrWhiteSpace(settings.AppDataPcPath) && !string.IsNullOrWhiteSpace(settings.AppDataBasePath))
        {
            settings.AppDataPcPath = BuildPcRuntimePath(settings.AppDataBasePath);
        }

        if (string.IsNullOrWhiteSpace(settings.AppDataBasePath) && !string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            settings.AppDataBasePath = TryDeriveBasePath(settings.AppDataPcPath);
        }
    }

    public static string BuildPcRuntimePath(string appDataBasePath)
    {
        var normalizedBase = Normalize(appDataBasePath);
        return Path.Combine(normalizedBase, VendorFolderName, GameFolderName, "XD", "PC");
    }

    public static string ResolveRuntimeDataPath(AppSettings settings)
    {
        NormalizeSettings(settings);

        if (!string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            return settings.AppDataPcPath;
        }

        throw new InvalidOperationException($"Informe a pasta AppData LocalLow\\{VendorFolderName}\\{GameFolderName}\\XD\\PC.");
    }

    public static string ResolveRuntimeRootPath(AppSettings settings)
    {
        NormalizeSettings(settings);

        if (!string.IsNullOrWhiteSpace(settings.AppDataBasePath))
        {
            return Path.Combine(settings.AppDataBasePath, VendorFolderName, GameFolderName);
        }

        if (!string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            return TryDeriveRuntimeRootFromPcPath(settings.AppDataPcPath);
        }

        throw new InvalidOperationException($"Informe a pasta AppData LocalLow\\{VendorFolderName}\\{GameFolderName}.");
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
            current.Parent.Parent?.Name.Equals(GameFolderName, StringComparison.OrdinalIgnoreCase) == true &&
            current.Parent.Parent.Parent?.Name.Equals(VendorFolderName, StringComparison.OrdinalIgnoreCase) == true &&
            current.Parent.Parent.Parent.Parent is not null)
        {
            return current.Parent.Parent.Parent.Parent.FullName;
        }

        return current.Parent?.FullName ?? normalized;
    }

    private static string TryDeriveRuntimeRootFromPcPath(string appDataPcPath)
    {
        var normalized = Normalize(appDataPcPath);
        var current = new DirectoryInfo(normalized);

        if (current.Name.Equals("PC", StringComparison.OrdinalIgnoreCase) &&
            current.Parent?.Name.Equals("XD", StringComparison.OrdinalIgnoreCase) == true &&
            current.Parent.Parent is not null)
        {
            return current.Parent.Parent.FullName;
        }

        return normalized;
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
