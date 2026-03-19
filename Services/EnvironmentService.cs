using RoLauncher.Models;
using System.IO;

namespace RoLauncher.Services;

public sealed class EnvironmentService
{
    public AccountProfile CreateEnvironment(AppSettings settings)
    {
        if (string.IsNullOrWhiteSpace(settings.GameInstallPath))
            throw new InvalidOperationException("Informe o caminho da instalação do jogo.");

        int next = settings.Accounts
            .Select(a => a.SlotNumber)
            .DefaultIfEmpty(0)
            .Max() + 1;

        string code = $"ro_win{next}";

        string root = string.IsNullOrWhiteSpace(settings.InstancesRootPath)
            ? Directory.GetParent(settings.GameInstallPath)!.FullName
            : settings.InstancesRootPath;

        string targetPath = Path.Combine(root, code);

        CopyDirectory(settings.GameInstallPath, targetPath);

        string exePath = Directory.GetFiles(targetPath, "*.exe", SearchOption.TopDirectoryOnly)
            .FirstOrDefault() ?? throw new FileNotFoundException("Executável não encontrado na pasta clonada.");

        string backupTokenFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoLauncher",
            "Accounts",
            code,
            "tokens");

        Directory.CreateDirectory(backupTokenFolder);

        return new AccountProfile
        {
            SlotNumber = next,
            Code = code,
            DisplayName = code,
            InstanceFolderPath = targetPath,
            ExecutablePath = exePath,
            BackupTokenFolderPath = backupTokenFolder,
            CreatedAt = DateTime.Now
        };
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (string dir in Directory.GetDirectories(source, "*", SearchOption.AllDirectories))
            Directory.CreateDirectory(dir.Replace(source, target));

        foreach (string file in Directory.GetFiles(source, "*.*", SearchOption.AllDirectories))
        {
            string targetFile = file.Replace(source, target);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            File.Copy(file, targetFile, true);
        }
    }
}