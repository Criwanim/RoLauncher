using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RoLauncher.Models;
using RoLauncher.Services;

namespace RoLauncher.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = new();
    private readonly EnvironmentService _environmentService = new();
    private readonly ShortcutService _shortcutService = new();
    private readonly TokenService _tokenService = new();
    private readonly GameLaunchService _gameLaunchService = new();

    private AppSettings _settings = new();

    public ObservableCollection<AccountProfile> Accounts { get; } = new();

    // 🎮 Caminho do jogo
    [ObservableProperty]
    private string gameInstallPath = string.Empty;

    // 📁 AppData Base
    [ObservableProperty]
    private string appDataBasePath = string.Empty;

    // 📁 AppData PC (XD\PC)
    [ObservableProperty]
    private string appDataPcPath = string.Empty;

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private AccountProfile? selectedAccount;

    public SetupViewModel()
    {
        _settings = _settingsService.Load();

        GameInstallPath = _settings.GameInstallPath;
        AppDataBasePath = _settings.AppDataBasePath;
        AppDataPcPath = _settings.AppDataPcPath;

        foreach (var account in _settings.Accounts.OrderBy(a => a.SlotNumber))
            Accounts.Add(account);
    }

    // =========================================================
    // 🎮 BROWSE GAME PATH
    // =========================================================

    [RelayCommand]
    private void BrowseGameInstallPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta de instalação do jogo"
        };

        if (dialog.ShowDialog() == true)
            GameInstallPath = dialog.FolderName;
    }

    // =========================================================
    // 📁 BROWSE APPDATA BASE
    // =========================================================

    [RelayCommand]
    private void BrowseAppDataBasePath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta base do AppData (LocalLow)"
        };

        if (dialog.ShowDialog() == true)
            AppDataBasePath = dialog.FolderName;
    }

    // =========================================================
    // 📁 BROWSE APPDATA PC
    // =========================================================

    [RelayCommand]
    private void BrowseAppDataPcPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta XD\\PC"
        };

        if (dialog.ShowDialog() == true)
            AppDataPcPath = dialog.FolderName;
    }

    // =========================================================
    // 💾 SALVAR CAMINHOS
    // =========================================================

    [RelayCommand]
    private void SavePaths()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.AppDataBasePath = AppDataBasePath;
        _settings.AppDataPcPath = AppDataPcPath;

        _settingsService.Save(_settings);

        Status = "Caminhos salvos com sucesso.";
    }

    // =========================================================
    // 🆕 CRIAR NOVA CONFIGURAÇÃO (NOVA CONTA)
    // =========================================================

    [RelayCommand]
    private void NewConfiguration()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.AppDataBasePath = AppDataBasePath;
        _settings.AppDataPcPath = AppDataPcPath;

        var account = _environmentService.CreateEnvironment(_settings);
        account.ShortcutPath = _shortcutService.CreateDesktopShortcut(account);

        _settings.Accounts.Add(account);
        _settingsService.Save(_settings);

        Accounts.Add(account);
        SelectedAccount = account;

        _gameLaunchService.Start(account.ExecutablePath);

        Status = $"{account.Code} criado. Faça login no jogo e depois clique em 'Capturar tokens'.";
    }

    // =========================================================
    // 🔐 CAPTURAR TOKENS
    // =========================================================

    [RelayCommand(CanExecute = nameof(CanCaptureTokens))]
    private void CaptureTokens()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.AppDataBasePath = AppDataBasePath;
        _settings.AppDataPcPath = AppDataPcPath;

        _tokenService.Capture(_settings, SelectedAccount!);
        _settingsService.Save(_settings);

        Status = $"Tokens da conta {SelectedAccount!.Code} capturados com sucesso.";
    }

    private bool CanCaptureTokens()
    {
        return SelectedAccount is not null;
    }
}