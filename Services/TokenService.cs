using RoLauncher.Models;
using System.IO;

namespace RoLauncher.Services;

public sealed class TokenService
{
    public void Capture(AppSettings settings, AccountProfile account)
    {
        EnsureRules(settings);
        Directory.CreateDirectory(account.BackupTokenFolderPath);

        foreach (var rule in settings.TokenRules)
        {
            string source = Path.Combine(settings.RuntimeTokenPath, rule.RuntimeFileName);
            string target = Path.Combine(account.BackupTokenFolderPath, rule.BackupFileName);

            if (!File.Exists(source))
                throw new FileNotFoundException($"Arquivo não encontrado: {source}");

            File.Copy(source, target, true);
        }
    }

    public void Restore(AppSettings settings, AccountProfile account)
    {
        EnsureRules(settings);

        foreach (var rule in settings.TokenRules)
        {
            string source = Path.Combine(account.BackupTokenFolderPath, rule.BackupFileName);
            string target = Path.Combine(settings.RuntimeTokenPath, rule.RuntimeFileName);

            if (!File.Exists(source))
                throw new FileNotFoundException($"Backup não encontrado: {source}");

            File.Copy(source, target, true);
        }
    }

    private static void EnsureRules(AppSettings settings)
    {
        if (settings.TokenRules.Count == 0)
            throw new InvalidOperationException("Defina os arquivos de sessão em TokenRules.");
    }
}