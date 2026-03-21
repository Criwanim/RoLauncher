using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class TokenService
{
    public void Capture(AppSettings settings, AccountProfile account)
    {
        var runtimeDataPath = PathLayoutService.ResolveRuntimeDataPath(settings);
        EnsureRules(settings);
        EnsureDirectoryExists(runtimeDataPath, "A pasta AppData LocalLow\\XD\\PC não foi encontrada.");
        Directory.CreateDirectory(account.BackupTokenFolderPath);

        foreach (var rule in settings.TokenRules)
        {
            var source = Path.Combine(runtimeDataPath, rule.RuntimeFileName);
            var target = Path.Combine(account.BackupTokenFolderPath, rule.BackupFileName);
            CopyFile(source, target, "Arquivo de sessão não encontrado");
        }
    }

    public void Restore(AppSettings settings, AccountProfile account)
    {
        var runtimeDataPath = PathLayoutService.ResolveRuntimeDataPath(settings);
        EnsureRules(settings);
        EnsureDirectoryExists(runtimeDataPath, "A pasta AppData LocalLow\\XD\\PC não foi encontrada.");

        foreach (var rule in settings.TokenRules)
        {
            var source = Path.Combine(account.BackupTokenFolderPath, rule.BackupFileName);
            var target = Path.Combine(runtimeDataPath, rule.RuntimeFileName);
            CopyFile(source, target, "Backup do token não encontrado");
        }
    }

    private static void EnsureRules(AppSettings settings)
    {
        if (settings.TokenRules.Count == 0)
        {
            throw new InvalidOperationException("Defina os arquivos de sessão em TokenRules no settings.json.");
        }
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
