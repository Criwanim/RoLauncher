using RoLauncher.Models;
using System.IO;

namespace RoLauncher.Services;

public sealed class SetupService
{
    private readonly AppSettings _settings;
    private readonly Action<AppSettings>? _saveSettingsAction;

    public SetupService(AppSettings settings, Action<AppSettings>? saveSettingsAction = null)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _saveSettingsAction = saveSettingsAction;
    }

    public AccountProfile CreateNewConfig()
    {
        string gameFolder = _settings.GameInstallPath;

        if (string.IsNullOrWhiteSpace(gameFolder))
            throw new InvalidOperationException("O caminho da pasta do jogo não foi configurado.");

        if (!Directory.Exists(gameFolder))
            throw new DirectoryNotFoundException($"A pasta do jogo não foi encontrada: {gameFolder}");

        string baseExe = Path.Combine(gameFolder, "ro_win.exe");
        string baseData = Path.Combine(gameFolder, "ro_win_Data");

        string masterExe = Path.Combine(gameFolder, "ro_win1.exe");
        string masterData = Path.Combine(gameFolder, "ro_win1_Data");

        AccountProfile profile;

        // PRIMEIRA CONFIGURAÇÃO
        // Se ainda não existe ro_win1, então renomeia a instalação base:
        // ro_win.exe      -> ro_win1.exe
        // ro_win_Data     -> ro_win1_Data
        if (!File.Exists(masterExe) && !Directory.Exists(masterData))
        {
            ValidateBaseInstallation(baseExe, baseData);

            File.Move(baseExe, masterExe);
            Directory.Move(baseData, masterData);

            profile = RegisterAccount(1, gameFolder);
            PersistSettings();

            return profile;
        }

        // A partir da segunda configuração, ro_win1 é a matriz/base
        ValidateMasterInstallation(masterExe, masterData);

        int nextSlot = GetNextSlot(gameFolder);

        string newExe = Path.Combine(gameFolder, $"ro_win{nextSlot}.exe");
        string newData = Path.Combine(gameFolder, $"ro_win{nextSlot}_Data");

        if (File.Exists(newExe) || Directory.Exists(newData))
            throw new InvalidOperationException(
                $"Os arquivos da configuração {nextSlot} já existem no diretório do jogo.");

        CopyFile(masterExe, newExe);
        CopyDirectory(masterData, newData);

        profile = RegisterAccount(nextSlot, gameFolder);
        PersistSettings();

        return profile;
    }

    public int GetNextSlot()
    {
        string gameFolder = _settings.GameInstallPath;

        if (string.IsNullOrWhiteSpace(gameFolder))
            throw new InvalidOperationException("O caminho da pasta do jogo não foi configurado.");

        if (!Directory.Exists(gameFolder))
            throw new DirectoryNotFoundException($"A pasta do jogo não foi encontrada: {gameFolder}");

        return GetNextSlot(gameFolder);
    }

    private int GetNextSlot(string gameFolder)
    {
        int slot = 1;

        while (true)
        {
            string exePath = Path.Combine(gameFolder, $"ro_win{slot}.exe");
            string dataPath = Path.Combine(gameFolder, $"ro_win{slot}_Data");

            bool exeExists = File.Exists(exePath);
            bool dataExists = Directory.Exists(dataPath);

            if (!exeExists && !dataExists)
                return slot;

            slot++;
        }
    }

    private static void ValidateBaseInstallation(string baseExe, string baseData)
    {
        if (!File.Exists(baseExe) && !Directory.Exists(baseData))
        {
            throw new InvalidOperationException(
                "Não foi encontrada a instalação base do jogo. " +
                "Esperado: ro_win.exe e ro_win_Data.");
        }

        if (!File.Exists(baseExe))
            throw new FileNotFoundException("Arquivo base não encontrado: ro_win.exe", baseExe);

        if (!Directory.Exists(baseData))
            throw new DirectoryNotFoundException("Pasta base não encontrada: ro_win_Data");
    }

    private static void ValidateMasterInstallation(string masterExe, string masterData)
    {
        if (!File.Exists(masterExe))
            throw new FileNotFoundException("Arquivo matriz não encontrado: ro_win1.exe", masterExe);

        if (!Directory.Exists(masterData))
            throw new DirectoryNotFoundException("Pasta matriz não encontrada: ro_win1_Data");
    }

    private static void CopyFile(string sourceFile, string destinationFile)
    {
        if (!File.Exists(sourceFile))
            throw new FileNotFoundException("Arquivo de origem não encontrado.", sourceFile);

        File.Copy(sourceFile, destinationFile, overwrite: false);
    }

    private static void CopyDirectory(string sourceDir, string destinationDir)
    {
        var source = new DirectoryInfo(sourceDir);

        if (!source.Exists)
            throw new DirectoryNotFoundException($"Pasta de origem não encontrada: {sourceDir}");

        Directory.CreateDirectory(destinationDir);

        foreach (FileInfo file in source.GetFiles())
        {
            string destinationFile = Path.Combine(destinationDir, file.Name);
            file.CopyTo(destinationFile, overwrite: false);
        }

        foreach (DirectoryInfo subDir in source.GetDirectories())
        {
            string newDestinationDir = Path.Combine(destinationDir, subDir.Name);
            CopyDirectory(subDir.FullName, newDestinationDir);
        }
    }

    private AccountProfile RegisterAccount(int slot, string gameFolder)
    {
        string code = $"ro_win{slot}";
        string exePath = Path.Combine(gameFolder, $"{code}.exe");
        string dataPath = Path.Combine(gameFolder, $"{code}_Data");

        AccountProfile? existing = _settings.Accounts
            .FirstOrDefault(a => a.SlotNumber == slot || a.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
            return existing;

        var profile = new AccountProfile
        {
            SlotNumber = slot,
            Code = code,
            DisplayName = $"Conta {slot}",
            InstanceFolderPath = dataPath,
            ExecutablePath = exePath,
            ShortcutPath = string.Empty,
            BackupTokenFolderPath = string.Empty,
            CreatedAt = DateTime.Now,
            LastLaunchAt = null
        };

        _settings.Accounts.Add(profile);

        return profile;
    }

    private void PersistSettings()
    {
        _saveSettingsAction?.Invoke(_settings);
    }
}