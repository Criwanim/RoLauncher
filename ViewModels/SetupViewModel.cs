using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RoLauncher.Models;
using RoLauncher.Services;
using System.IO;

namespace RoLauncher.ViewModels;

public partial class SetupViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly EnvironmentService _environmentService;
    private readonly ShortcutService _shortcutService;
    private readonly TokenService _tokenService;
    private readonly GameLaunchService _gameLaunchService;
    private AppSettings _settings;

    public SetupViewModel()
        : this(
            new SettingsService(),
            new EnvironmentService(),
            new ShortcutService(),
            new TokenService(),
            new GameLaunchService())
    {
    }

    internal SetupViewModel(
        SettingsService settingsService,
        EnvironmentService environmentService,
        ShortcutService shortcutService,
        TokenService tokenService,
        GameLaunchService gameLaunchService)
    {
        _settingsService = settingsService;
        _environmentService = environmentService;
        _shortcutService = shortcutService;
        _tokenService = tokenService;
        _gameLaunchService = gameLaunchService;

        _settings = _settingsService.Load();
        PathLayoutService.NormalizeSettings(_settings);

        GameInstallPath = _settings.GameInstallPath;
        AppDataBasePath = _settings.AppDataBasePath;
        AppDataPcPath = _settings.AppDataPcPath;
        InstancesRootPath = _settings.InstancesRootPath;

        foreach (var account in _settings.Accounts.OrderBy(account => account.SlotNumber))
        {
            Accounts.Add(account);
        }

        Status = Accounts.Count == 0
            ? $"Pronto. Arquivo de configuração: {_settingsService.SettingsFilePath}"
            : $"{Accounts.Count} conta(s) carregada(s).";
    }

    public ObservableCollection<AccountProfile> Accounts { get; } = new();

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewConfigurationCommand))]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private string gameInstallPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewConfigurationCommand))]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private string appDataBasePath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewConfigurationCommand))]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private string appDataPcPath = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(NewConfigurationCommand))]
    private string instancesRootPath = string.Empty;

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private AccountProfile? selectedAccount;

    [RelayCommand]
    private void BrowseGameInstallPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta de instalação do jogo"
        };

        if (dialog.ShowDialog() == true)
        {
            GameInstallPath = dialog.FolderName;

            if (string.IsNullOrWhiteSpace(InstancesRootPath))
            {
                InstancesRootPath = PathLayoutService.ResolveDefaultInstancesRoot(GameInstallPath);
            }
        }
    }

    [RelayCommand]
    private void BrowseAppDataBasePath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta base do AppData LocalLow"
        };

        if (dialog.ShowDialog() == true)
        {
            AppDataBasePath = dialog.FolderName;

            if (string.IsNullOrWhiteSpace(AppDataPcPath))
            {
                AppDataPcPath = Path.Combine(AppDataBasePath, "XD", "PC");
            }
        }
    }

    [RelayCommand]
    private void BrowseAppDataPcPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta AppData LocalLow\\XD\\PC"
        };

        if (dialog.ShowDialog() == true)
        {
            AppDataPcPath = dialog.FolderName;

            if (string.IsNullOrWhiteSpace(AppDataBasePath))
            {
                AppDataBasePath = PathLayoutService.TryDeriveBasePath(AppDataPcPath);
            }
        }
    }

    [RelayCommand]
    private void BrowseInstancesRootPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta raiz das instâncias clonadas"
        };

        if (dialog.ShowDialog() == true)
        {
            InstancesRootPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void SavePaths()
    {
        ApplyScreenValuesToSettings();
        _settingsService.Save(_settings);
        RefreshFieldsFromSettings();
        Status = "Caminhos salvos com sucesso.";
    }

    [RelayCommand(CanExecute = nameof(CanCreateConfiguration))]
    private void NewConfiguration()
    {
        ApplyScreenValuesToSettings();

        var account = _environmentService.CreateEnvironment(_settings);
        account.ShortcutPath = _shortcutService.CreateDesktopShortcut(account);

        _settings.Accounts.Add(account);
        _settingsService.Save(_settings);

        Accounts.Add(account);
        SelectedAccount = account;
        _gameLaunchService.Start(account.ExecutablePath);

        Status = $"{account.Code} criado. Faça login no jogo e depois clique em 'Capturar tokens'.";
    }

    private bool CanCreateConfiguration()
    {
        return !string.IsNullOrWhiteSpace(GameInstallPath)
            && !string.IsNullOrWhiteSpace(AppDataBasePath)
            && !string.IsNullOrWhiteSpace(AppDataPcPath)
            && !string.IsNullOrWhiteSpace(InstancesRootPath);
    }

    [RelayCommand(CanExecute = nameof(CanCaptureTokens))]
    private void CaptureTokens()
    {
        ApplyScreenValuesToSettings();
        _tokenService.Capture(_settings, SelectedAccount!);
        _settingsService.Save(_settings);
        Status = $"Tokens da conta {SelectedAccount!.Code} capturados com sucesso.";
    }

    private bool CanCaptureTokens()
    {
        return SelectedAccount is not null
            && !string.IsNullOrWhiteSpace(AppDataPcPath)
            && !string.IsNullOrWhiteSpace(AppDataBasePath);
    }

    private void ApplyScreenValuesToSettings()
    {
        _settings.GameInstallPath = GameInstallPath;
        _settings.AppDataBasePath = AppDataBasePath;
        _settings.AppDataPcPath = AppDataPcPath;
        _settings.InstancesRootPath = InstancesRootPath;
        PathLayoutService.NormalizeSettings(_settings);
    }

    private void RefreshFieldsFromSettings()
    {
        GameInstallPath = _settings.GameInstallPath;
        AppDataBasePath = _settings.AppDataBasePath;
        AppDataPcPath = _settings.AppDataPcPath;
        InstancesRootPath = _settings.InstancesRootPath;
    }
}
