using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using RoLauncher.Models;
using RoLauncher.Services;

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

        TryAutoDetectPaths();
        PathLayoutService.NormalizeSettings(_settings);

        GameInstallPath = _settings.GameInstallPath;
        AppDataBasePath = _settings.AppDataBasePath;
        AppDataPcPath = _settings.AppDataPcPath;

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
    private string status = "Pronto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private AccountProfile? selectedAccount;

    [RelayCommand]
    private void BrowseGameInstallPath()
    {
        TryAutoDetectGameInstallPath();

        if (!string.IsNullOrWhiteSpace(_settings.GameInstallPath) &&
            Directory.Exists(_settings.GameInstallPath))
        {
            GameInstallPath = _settings.GameInstallPath;
            Status = "Pasta do jogo localizada automaticamente.";
            return;
        }

        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta de instalação do jogo"
        };

        if (dialog.ShowDialog() == true)
        {
            GameInstallPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseAppDataBasePath()
    {
        TryAutoDetectAppDataPaths();

        if (!string.IsNullOrWhiteSpace(_settings.AppDataBasePath) &&
            Directory.Exists(_settings.AppDataBasePath))
        {
            AppDataBasePath = _settings.AppDataBasePath;

            if (string.IsNullOrWhiteSpace(AppDataPcPath) &&
                !string.IsNullOrWhiteSpace(_settings.AppDataPcPath))
            {
                AppDataPcPath = _settings.AppDataPcPath;
            }

            Status = "Pasta base do AppData localizada automaticamente.";
            return;
        }

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
        CreateConfigurationWithAlias(null);
    }

    public void CreateConfigurationWithAlias(string? alias)
    {
        try
        {
            ApplyScreenValuesToSettings();

            var account = _environmentService.CreateEnvironment(_settings);
            account.DisplayName = string.IsNullOrWhiteSpace(alias)
                ? account.Code
                : alias.Trim();
            account.ShortcutPath = _shortcutService.CreateDesktopShortcut(account);

            _settings.Accounts.Add(account);
            _settingsService.Save(_settings);

            Accounts.Add(account);
            SelectedAccount = account;
            _gameLaunchService.Start(account.ExecutablePath);

            Status = $"{account.Code} criado. Faça login no jogo e depois clique em 'Capturar tokens'.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao criar configuração: {ex.Message}";
        }
    }

    private bool CanCreateConfiguration()
    {
        return !string.IsNullOrWhiteSpace(GameInstallPath)
            && !string.IsNullOrWhiteSpace(AppDataBasePath)
            && !string.IsNullOrWhiteSpace(AppDataPcPath);
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

        PathLayoutService.NormalizeSettings(_settings);
    }

    private void RefreshFieldsFromSettings()
    {
        GameInstallPath = _settings.GameInstallPath;
        AppDataBasePath = _settings.AppDataBasePath;
        AppDataPcPath = _settings.AppDataPcPath;
    }

    private void TryAutoDetectPaths()
    {
        TryAutoDetectAppDataPaths();
        TryAutoDetectGameInstallPath();
    }

    private void TryAutoDetectAppDataPaths()
    {
        if (string.IsNullOrWhiteSpace(_settings.AppDataBasePath))
        {
            var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var localLowPath = Path.GetFullPath(Path.Combine(localAppData, "..", "LocalLow"));

            if (Directory.Exists(localLowPath))
            {
                _settings.AppDataBasePath = localLowPath;
            }
        }

        if (string.IsNullOrWhiteSpace(_settings.AppDataPcPath) &&
            !string.IsNullOrWhiteSpace(_settings.AppDataBasePath))
        {
            var candidate = Path.Combine(_settings.AppDataBasePath, "XD", "PC");

            if (Directory.Exists(candidate))
            {
                _settings.AppDataPcPath = candidate;
            }
        }
    }

    private void TryAutoDetectGameInstallPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.GameInstallPath) &&
            Directory.Exists(_settings.GameInstallPath))
        {
            return;
        }

        var exactCandidates = new[]
        {
            @"C:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"D:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"E:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"F:\Program Files (x86)\XD\Ragnarok M Classic Global"
        };

        foreach (var candidate in exactCandidates)
        {
            if (!Directory.Exists(candidate))
            {
                continue;
            }

            var executables = new[]
            {
                Path.Combine(candidate, "ro_win.exe"),
                Path.Combine(candidate, "ro_win1.exe"),
                Path.Combine(candidate, "launcher.exe"),
                Path.Combine(candidate, "Launcher.exe")
            };

            if (executables.Any(File.Exists))
            {
                _settings.GameInstallPath = candidate;
                return;
            }

            _settings.GameInstallPath = candidate;
            return;
        }
    }
}
