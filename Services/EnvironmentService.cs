using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RoLauncher.Models;

namespace RoLauncher.Services;

public class EnvironmentService
{
    public AccountProfile CreateEnvironment(AppSettings settings, string? alias = null)
    {
        if (string.IsNullOrWhiteSpace(settings.GameInstallPath))
        {
            throw new InvalidOperationException("A pasta de instalação do jogo não foi informada.");
        }

        if (!Directory.Exists(settings.GameInstallPath))
        {
            throw new DirectoryNotFoundException("A pasta de instalação do jogo não foi encontrada.");
        }

        if (string.IsNullOrWhiteSpace(settings.AppDataPcPath))
        {
            throw new InvalidOperationException("A pasta AppData LocalLow do jogo não foi informada.");
        }

        var gameFolder = settings.GameInstallPath;
        var nextSlot = GetNextSlot(settings);
        var code = $"ro_win{nextSlot}";

        EnsureBaseInstanceExists(gameFolder);

        if (nextSlot == 1)
        {
            PrepareFirstInstance(gameFolder);
        }
        else
        {
            CreateClonedInstance(gameFolder, nextSlot);
        }

        var executablePath = Path.Combine(gameFolder, $"{code}.exe");
        var backupTokenFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "RoLauncher",
            "Tokens",
            code);

        Directory.CreateDirectory(backupTokenFolderPath);

        var normalizedAlias = string.IsNullOrWhiteSpace(alias) ? code : alias.Trim();

        return new AccountProfile
        {
            SlotNumber = nextSlot,
            Code = code,
            DisplayName = normalizedAlias,
            InstanceFolderPath = gameFolder,
            ExecutablePath = executablePath,
            BackupTokenFolderPath = backupTokenFolderPath,
            CreatedAt = DateTime.Now
        };
    }

    private static int GetNextSlot(AppSettings settings)
    {
        var occupiedSlots = GetOccupiedSlots(settings.GameInstallPath);
        var nextSlot = 1;

        while (occupiedSlots.Contains(nextSlot))
        {
            nextSlot++;
        }

        return nextSlot;
    }

    private static HashSet<int> GetOccupiedSlots(string gameFolder)
    {
        var occupiedSlots = new HashSet<int>();

        if (!Directory.Exists(gameFolder))
        {
            return occupiedSlots;
        }

        foreach (var executablePath in Directory.EnumerateFiles(gameFolder, "ro_win*.exe", SearchOption.TopDirectoryOnly))
        {
            if (TryParseSlot(Path.GetFileNameWithoutExtension(executablePath), out var slot))
            {
                occupiedSlots.Add(slot);
            }
        }

        foreach (var dataDirectoryPath in Directory.EnumerateDirectories(gameFolder, "ro_win*_Data", SearchOption.TopDirectoryOnly))
        {
            var directoryName = Path.GetFileName(dataDirectoryPath);
            if (directoryName.EndsWith("_Data", StringComparison.OrdinalIgnoreCase) &&
                TryParseSlot(directoryName[..^5], out var slot))
            {
                occupiedSlots.Add(slot);
            }
        }

        return occupiedSlots;
    }

    private static bool TryParseSlot(string code, out int slot)
    {
        slot = 0;
        if (!code.StartsWith("ro_win", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return int.TryParse(code[6..], out slot) && slot > 0;
    }

    private static void EnsureBaseInstanceExists(string gameFolder)
    {
        var originalExe = Path.Combine(gameFolder, "ro_win.exe");
        var originalData = Path.Combine(gameFolder, "ro_win_Data");

        var firstExe = Path.Combine(gameFolder, "ro_win1.exe");
        var firstData = Path.Combine(gameFolder, "ro_win1_Data");

        var originalExists = File.Exists(originalExe) && Directory.Exists(originalData);
        var firstExists = File.Exists(firstExe) && Directory.Exists(firstData);

        if (!originalExists && !firstExists)
        {
            throw new FileNotFoundException(
                "Nenhuma instância base foi encontrada. Esperado: ro_win.exe/ro_win_Data ou ro_win1.exe/ro_win1_Data.");
        }
    }

    private static void PrepareFirstInstance(string gameFolder)
    {
        var originalExe = Path.Combine(gameFolder, "ro_win.exe");
        var originalData = Path.Combine(gameFolder, "ro_win_Data");

        var firstExe = Path.Combine(gameFolder, "ro_win1.exe");
        var firstData = Path.Combine(gameFolder, "ro_win1_Data");

        if (File.Exists(firstExe) && Directory.Exists(firstData))
        {
            return;
        }

        if (!File.Exists(originalExe))
        {
            throw new FileNotFoundException("Arquivo base ro_win.exe não encontrado.");
        }

        if (!Directory.Exists(originalData))
        {
            throw new DirectoryNotFoundException("Pasta base ro_win_Data não encontrada.");
        }

        File.Move(originalExe, firstExe);
        Directory.Move(originalData, firstData);
    }

    private static void CreateClonedInstance(string gameFolder, int slot)
    {
        var sourceExe = Path.Combine(gameFolder, "ro_win1.exe");
        var sourceData = Path.Combine(gameFolder, "ro_win1_Data");

        var targetExe = Path.Combine(gameFolder, $"ro_win{slot}.exe");
        var targetData = Path.Combine(gameFolder, $"ro_win{slot}_Data");

        if (!File.Exists(sourceExe))
        {
            throw new FileNotFoundException("Arquivo base ro_win1.exe não encontrado.");
        }

        if (!Directory.Exists(sourceData))
        {
            throw new DirectoryNotFoundException("Pasta base ro_win1_Data não encontrada.");
        }

        if (File.Exists(targetExe) || Directory.Exists(targetData))
        {
            throw new InvalidOperationException($"A instância ro_win{slot} já existe.");
        }

        File.Copy(sourceExe, targetExe, overwrite: false);
        CopyDirectory(sourceData, targetData);
    }

    private static void CopyDirectory(string sourceDir, string targetDir)
    {
        var source = new DirectoryInfo(sourceDir);

        if (!source.Exists)
        {
            throw new DirectoryNotFoundException($"Diretório de origem não encontrado: {sourceDir}");
        }

        Directory.CreateDirectory(targetDir);

        foreach (var file in source.GetFiles())
        {
            var targetFilePath = Path.Combine(targetDir, file.Name);
            file.CopyTo(targetFilePath, overwrite: false);
        }

        foreach (var directory in source.GetDirectories())
        {
            var nextTargetSubDir = Path.Combine(targetDir, directory.Name);
            CopyDirectory(directory.FullName, nextTargetSubDir);
        }
    }
}
