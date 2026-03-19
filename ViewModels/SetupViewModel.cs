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

    [ObservableProperty]
    private string gameInstallPath = string.Empty;

    [ObservableProperty]
    private string runtimeTokenPath = string.Empty;

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
private AccountProfile? selectedAccount;

    public SetupViewModel()
    {
        _settings = _settingsService.Load();

        GameInstallPath = _settings.GameInstallPath;
        RuntimeTokenPath = _settings.RuntimeTokenPath;

        foreach (var account in _settings.Accounts.OrderBy(a => a.SlotNumber))
            Accounts.Add(account);
    }

    [RelayCommand]
    private void BrowseGameInstallPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta de instalação do jogo"
        };

        bool? result = dialog.ShowDialog();

        if (result == true)
            GameInstallPath = dialog.FolderName;
    }

    [RelayCommand]
    private void BrowseRuntimeTokenPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta dos tokens"
        };

        bool? result = dialog.ShowDialog();

        if (result == true)
            RuntimeTokenPath = dialog.FolderName;
    }

    [RelayCommand]
    private void SavePaths()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.RuntimeTokenPath = RuntimeTokenPath;

        _settingsService.Save(_settings);
        Status = "Caminhos salvos com sucesso.";
    }

    [RelayCommand]
    private void NewConfiguration()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.RuntimeTokenPath = RuntimeTokenPath;

        var account = _environmentService.CreateEnvironment(_settings);
        account.ShortcutPath = _shortcutService.CreateDesktopShortcut(account);

        _settings.Accounts.Add(account);
        _settingsService.Save(_settings);

        Accounts.Add(account);
        SelectedAccount = account;

        _gameLaunchService.Start(account.ExecutablePath);

        Status = $"{account.Code} criado. Faça login no jogo e depois clique em 'Capturar tokens'.";
    }

    [RelayCommand(CanExecute = nameof(CanCaptureTokens))]
    private void CaptureTokens()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.RuntimeTokenPath = RuntimeTokenPath;

        _tokenService.Capture(_settings, SelectedAccount!);
        _settingsService.Save(_settings);

        Status = $"Tokens da conta {SelectedAccount!.Code} capturados com sucesso.";
    }

    private bool CanCaptureTokens()
    {
        return SelectedAccount is not null;
    }
}