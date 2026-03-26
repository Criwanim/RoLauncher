using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class TokenService
{
    private static readonly string[] RootRuntimeFiles = [".userdata"];
    private static readonly string[] PcRuntimeFiles = ["access_token_v2", "user_v2"];

    public void Capture(AppSettings settings, AccountProfile account)
    {
        var runtimeRootPath = PathLayoutService.ResolveRuntimeRootPath(settings);
        var runtimePcPath = PathLayoutService.ResolveRuntimeDataPath(settings);

        EnsureDirectoryExists(runtimeRootPath, "A pasta de runtime do LocalLow não foi encontrada.");
        EnsureDirectoryExists(runtimePcPath, "A pasta AppData LocalLow do jogo não foi encontrada.");

        Directory.CreateDirectory(account.BackupTokenFolderPath);

        foreach (var fileName in RootRuntimeFiles)
        {
            var source = Path.Combine(runtimeRootPath, fileName);
            var target = Path.Combine(account.BackupTokenFolderPath, BuildBackupFileName(fileName, account.SlotNumber));
            CopyFile(source, target, "Arquivo de sessão não encontrado");
        }

        foreach (var fileName in PcRuntimeFiles)
        {
            var source = Path.Combine(runtimePcPath, fileName);
            var target = Path.Combine(account.BackupTokenFolderPath, BuildBackupFileName(fileName, account.SlotNumber));
            CopyFile(source, target, "Arquivo de sessão não encontrado");
        }
    }

    public void Restore(AppSettings settings, AccountProfile account)
    {
        var runtimeRootPath = PathLayoutService.ResolveRuntimeRootPath(settings);
        var runtimePcPath = PathLayoutService.ResolveRuntimeDataPath(settings);

        EnsureDirectoryExists(runtimeRootPath, "A pasta de runtime do LocalLow não foi encontrada.");
        EnsureDirectoryExists(runtimePcPath, "A pasta AppData LocalLow do jogo não foi encontrada.");

        foreach (var fileName in RootRuntimeFiles)
        {
            var source = ResolveBackupSource(account, fileName);
            var target = Path.Combine(runtimeRootPath, fileName);
            CopyFile(source, target, "Backup do token não encontrado");
        }

        foreach (var fileName in PcRuntimeFiles)
        {
            var source = ResolveBackupSource(account, fileName);
            var target = Path.Combine(runtimePcPath, fileName);
            CopyFile(source, target, "Backup do token não encontrado");
        }
    }

    private static string ResolveBackupSource(AccountProfile account, string fileName)
    {
        var preferred = Path.Combine(account.BackupTokenFolderPath, BuildBackupFileName(fileName, account.SlotNumber));
        if (File.Exists(preferred))
        {
            return preferred;
        }

        var legacy = Path.Combine(account.BackupTokenFolderPath, fileName);
        return legacy;
    }

    private static string BuildBackupFileName(string originalFileName, int slotNumber)
    {
        return $"{originalFileName}_{slotNumber}";
    }

    private static void EnsureDirectoryExists(string path, string message)
    {
        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException($"{message} Caminho: {path}");
        }
    }

    private static void CopyFile(string source, string target, string errorPrefix)
    {
        if (!File.Exists(source))
        {
            throw new FileNotFoundException($"{errorPrefix}: {source}");
        }

        var targetDirectory = Path.GetDirectoryName(target);
        if (!string.IsNullOrWhiteSpace(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        File.Copy(source, target, overwrite: true);
    }
}
