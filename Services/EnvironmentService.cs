using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class EnvironmentService
{
    public AccountProfile CreateEnvironment(AppSettings settings)
    {
        PathLayoutService.NormalizeSettings(settings);

        if (string.IsNullOrWhiteSpace(settings.GameInstallPath) || !Directory.Exists(settings.GameInstallPath))
        {
            throw new InvalidOperationException("Informe uma pasta válida de instalação do jogo.");
        }

        var nextSlot = settings.Accounts
            .Select(account => account.SlotNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;

        var code = $"ro_win{nextSlot}";
        var instancesRoot = PathLayoutService.ResolveInstancesRoot(settings);
        Directory.CreateDirectory(instancesRoot);

        var targetPath = Path.Combine(instancesRoot, code);
        if (Directory.Exists(targetPath))
        {
            throw new InvalidOperationException($"A instância {code} já existe em {targetPath}.");
        }

        CopyDirectory(settings.GameInstallPath, targetPath);

        var executablePath = ResolveExecutable(targetPath);
        var backupTokenFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoLauncher",
            "Accounts",
            code,
            "tokens");

        Directory.CreateDirectory(backupTokenFolderPath);

        return new AccountProfile
        {
            SlotNumber = nextSlot,
            Code = code,
            DisplayName = code,
            InstanceFolderPath = targetPath,
            ExecutablePath = executablePath,
            BackupTokenFolderPath = backupTokenFolderPath,
            CreatedAt = DateTime.Now
        };
    }

    private static string ResolveExecutable(string instanceFolderPath)
    {
        var executables = Directory.GetFiles(instanceFolderPath, "*.exe", SearchOption.TopDirectoryOnly)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (executables.Count == 0)
        {
            throw new FileNotFoundException("Nenhum executável foi encontrado na instância clonada.");
        }

        return executables[0];
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var directory in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
        {
            Directory.CreateDirectory(directory.Replace(source, target, StringComparison.OrdinalIgnoreCase));
        }

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var targetFile = file.Replace(source, target, StringComparison.OrdinalIgnoreCase);
            var targetDirectory = Path.GetDirectoryName(targetFile);

            if (!string.IsNullOrWhiteSpace(targetDirectory))
            {
                Directory.CreateDirectory(targetDirectory);
            }

            File.Copy(file, targetFile, overwrite: true);
        }
    }
}
