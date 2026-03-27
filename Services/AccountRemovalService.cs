using System;
using System.IO;
using RoLauncher.Models;

namespace RoLauncher.Services;

public sealed class AccountRemovalService
{
    public void DeleteAccount(AppSettings settings, AccountProfile account)
    {
        if (settings is null)
            throw new ArgumentNullException(nameof(settings));

        if (account is null)
            throw new ArgumentNullException(nameof(account));

        if (account.SlotNumber == 1 ||
            string.Equals(account.Code, "ro_win1", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A conta ro_win1 não pode ser excluída porque ela é a instância base para criar novas contas.");
        }

        if (string.IsNullOrWhiteSpace(settings.GameInstallPath))
            throw new InvalidOperationException("A pasta do jogo não foi configurada.");

        if (!Directory.Exists(settings.GameInstallPath))
            throw new DirectoryNotFoundException("A pasta do jogo não foi encontrada.");

        var code = ResolveCode(account);
        var executablePath = !string.IsNullOrWhiteSpace(account.ExecutablePath)
            ? account.ExecutablePath
            : Path.Combine(settings.GameInstallPath, $"{code}.exe");
        var dataFolderPath = Path.Combine(settings.GameInstallPath, $"{code}_Data");

        DeleteFileIfExists(executablePath);
        DeleteDirectoryIfExists(dataFolderPath);
        DeleteDirectoryIfExists(account.BackupTokenFolderPath);
        DeleteFileIfExists(account.ShortcutPath);

        settings.Accounts.Remove(account);
    }

    private static string ResolveCode(AccountProfile account)
    {
        if (!string.IsNullOrWhiteSpace(account.Code))
            return account.Code.Trim();

        if (account.SlotNumber <= 0)
            throw new InvalidOperationException("A conta selecionada não possui código nem slot válido.");

        return $"ro_win{account.SlotNumber}";
    }

    private static void DeleteFileIfExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return;

        var attributes = File.GetAttributes(filePath);
        if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
        {
            File.SetAttributes(filePath, attributes & ~FileAttributes.ReadOnly);
        }

        File.Delete(filePath);
    }

    private static void DeleteDirectoryIfExists(string? directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
            return;

        ClearReadOnly(directoryPath);
        Directory.Delete(directoryPath, recursive: true);
    }

    private static void ClearReadOnly(string directoryPath)
    {
        foreach (var file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
        {
            var attributes = File.GetAttributes(file);
            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                File.SetAttributes(file, attributes & ~FileAttributes.ReadOnly);
            }
        }
    }
}
