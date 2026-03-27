using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        ReloadAccounts();

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
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private bool isBusy;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    private string progressMessage = string.Empty;

    [ObservableProperty]
    private string status = "Pronto";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(CaptureTokensCommand))]
    private AccountProfile? selectedAccount;

    [RelayCommand]
    private void BrowseGameInstallPath()
    {
        TryAutoDetectGameInstallPath();

        if (!string.IsNullOrWhiteSpace(_settings.GameInstallPath) && Directory.Exists(_settings.GameInstallPath))
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

        if (!string.IsNullOrWhiteSpace(_settings.AppDataBasePath) && Directory.Exists(_settings.AppDataBasePath))
        {
            AppDataBasePath = _settings.AppDataBasePath;
            AppDataPcPath = PathLayoutService.BuildPcRuntimePath(AppDataBasePath);
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
            AppDataPcPath = PathLayoutService.BuildPcRuntimePath(AppDataBasePath);
        }
    }

    [RelayCommand]
    private void BrowseAppDataPcPath()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
            Title = "Selecione a pasta XD\\PC do Ragnarok"
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

    [RelayCommand(CanExecute = nameof(CanStartNewConfiguration))]
    private async Task NewConfiguration()
    {
        await CreateConfigurationWithAliasAsync(null);
    }

    public bool CanStartNewConfiguration()
    {
        return !IsBusy &&
               !string.IsNullOrWhiteSpace(GameInstallPath) &&
               !string.IsNullOrWhiteSpace(AppDataBasePath) &&
               !string.IsNullOrWhiteSpace(AppDataPcPath);
    }

    public async Task CreateConfigurationWithAliasAsync(string? alias)
    {
        if (!CanStartNewConfiguration())
        {
            return;
        }

        try
        {
            IsBusy = true;
            ProgressValue = 0;
            ProgressMessage = "Preparando configuração...";
            Status = "Criando nova configuração...";

            ApplyScreenValuesToSettings();

            var progress = new Progress<EnvironmentProgress>(p =>
            {
                ProgressValue = p.Percentage;
                ProgressMessage = p.Message;
                Status = p.Message;
            });

            var account = await _environmentService.CreateEnvironmentAsync(_settings, progress, alias);

            account.ShortcutPath = _shortcutService.CreateDesktopShortcut(account);

            _settings.Accounts.Add(account);
            _settingsService.Save(_settings);
            ReloadAccounts();

            SelectedAccount = Accounts.FirstOrDefault(a => a.SlotNumber == account.SlotNumber);
            _gameLaunchService.Start(account.ExecutablePath);

            ProgressValue = 100;
            ProgressMessage = "Configuração criada com sucesso.";
            Status = $"{account.DisplayName} ({account.Code}) criado. Faça login no jogo e depois clique em 'Capturar tokens'.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao criar configuração: {ex.Message}";
            ProgressMessage = "Falha ao criar configuração.";
        }
        finally
        {
            IsBusy = false;
            NewConfigurationCommand.NotifyCanExecuteChanged();
            CaptureTokensCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand(CanExecute = nameof(CanCaptureTokens))]
    private void CaptureTokens()
    {
        try
        {
            ApplyScreenValuesToSettings();
            _tokenService.Capture(_settings, SelectedAccount!);
            _settingsService.Save(_settings);
            Status = $"Tokens da conta {SelectedAccount!.DisplayName} capturados com sucesso.";
        }
        catch (Exception ex)
        {
            Status = $"Erro ao capturar tokens: {ex.Message}";
        }
    }

    private bool CanCaptureTokens()
    {
        return !IsBusy &&
               SelectedAccount is not null &&
               !string.IsNullOrWhiteSpace(AppDataPcPath) &&
               !string.IsNullOrWhiteSpace(AppDataBasePath);
    }

    partial void OnIsBusyChanged(bool value)
    {
        NewConfigurationCommand.NotifyCanExecuteChanged();
        CaptureTokensCommand.NotifyCanExecuteChanged();
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

    private void ReloadAccounts()
    {
        Accounts.Clear();

        foreach (var account in _settings.Accounts.OrderBy(account => account.SlotNumber))
        {
            Accounts.Add(account);
        }
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

        if (!string.IsNullOrWhiteSpace(_settings.AppDataBasePath))
        {
            _settings.AppDataPcPath = PathLayoutService.BuildPcRuntimePath(_settings.AppDataBasePath);
        }
    }

    private void TryAutoDetectGameInstallPath()
    {
        if (!string.IsNullOrWhiteSpace(_settings.GameInstallPath) && Directory.Exists(_settings.GameInstallPath))
        {
            return;
        }

        var candidates = new[]
        {
            @"C:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"D:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"E:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"F:\Program Files (x86)\XD\Ragnarok M Classic Global",
            @"C:\XD\Ragnarok M Classic Global",
            @"D:\XD\Ragnarok M Classic Global",
            @"E:\XD\Ragnarok M Classic Global",
            @"F:\XD\Ragnarok M Classic Global"
        };

        _settings.GameInstallPath = candidates.FirstOrDefault(Directory.Exists) ?? _settings.GameInstallPath;
    }
}
