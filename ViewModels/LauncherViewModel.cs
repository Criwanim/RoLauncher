using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoLauncher.Services;

namespace RoLauncher.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    private readonly SettingsService _settingsService;
    private readonly TokenService _tokenService;
    private readonly GameLaunchService _gameLaunchService;
    private readonly Models.AppSettings _settings;
    private bool _isBusy;

    public LauncherViewModel()
        : this(new SettingsService(), new TokenService(), new GameLaunchService())
    {
    }

    internal LauncherViewModel(
        SettingsService settingsService,
        TokenService tokenService,
        GameLaunchService gameLaunchService)
    {
        _settingsService = settingsService;
        _tokenService = tokenService;
        _gameLaunchService = gameLaunchService;
        _settings = _settingsService.Load();

        foreach (var account in _settings.Accounts.OrderBy(account => account.SlotNumber))
        {
            var item = new LauncherAccountItemViewModel(account);
            item.PropertyChanged += OnAccountItemPropertyChanged;
            Accounts.Add(item);
        }

        Status = Accounts.Count == 0
            ? "Nenhuma conta configurada."
            : $"{Accounts.Count} conta(s) carregada(s).";
    }

    public ObservableCollection<LauncherAccountItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private string status = "Pronto";

    private void OnAccountItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherAccountItemViewModel.IsSelected))
        {
            RefreshCommandStates();
        }
    }

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        foreach (var item in Accounts)
        {
            item.IsSelected = true;
        }

        Status = "Todas as contas foram marcadas.";
        RefreshCommandStates();
    }

    private bool CanSelectAll() => !_isBusy && Accounts.Count > 0;

    [RelayCommand(CanExecute = nameof(CanClearSelection))]
    private void ClearSelection()
    {
        foreach (var item in Accounts)
        {
            item.IsSelected = false;
        }

        Status = "Seleção limpa.";
        RefreshCommandStates();
    }

    private bool CanClearSelection() => !_isBusy && Accounts.Any(account => account.IsSelected);

    [RelayCommand(CanExecute = nameof(CanOpenSelected), AllowConcurrentExecutions = false)]
    private async Task OpenSelectedAsync()
    {
        var selectedItems = Accounts
            .Where(account => account.IsSelected)
            .OrderBy(account => account.Account.SlotNumber)
            .ToList();

        await RunLaunchLoopAsync(selectedItems);
    }

    private bool CanOpenSelected() => !_isBusy && Accounts.Any(account => account.IsSelected);

    [RelayCommand(CanExecute = nameof(CanOpenAll), AllowConcurrentExecutions = false)]
    private async Task OpenAllAsync()
    {
        foreach (var item in Accounts)
        {
            item.IsSelected = true;
        }

        RefreshCommandStates();
        var allItems = Accounts.OrderBy(account => account.Account.SlotNumber).ToList();
        await RunLaunchLoopAsync(allItems);
    }

    private bool CanOpenAll() => !_isBusy && Accounts.Count > 0;

    private async Task RunLaunchLoopAsync(List<LauncherAccountItemViewModel> items)
    {
        if (items.Count == 0)
        {
            Status = "Nenhuma conta selecionada.";
            return;
        }

        _isBusy = true;
        RefreshCommandStates();

        try
        {
            foreach (var item in items)
            {
                try
                {
                    item.Status = "Restaurando tokens...";
                    Status = $"Restaurando sessão de {item.Code}...";
                    _tokenService.Restore(_settings, item.Account);

                    item.Status = "Abrindo jogo...";
                    Status = $"Abrindo {item.Code}...";

                    var process = _gameLaunchService.Start(item.Account.ExecutablePath)
                        ?? throw new InvalidOperationException($"Falha ao iniciar {item.Code}.");

                    await WaitUntilStartedAsync(process);
                    item.Account.LastLaunchAt = DateTime.Now;
                    item.Refresh();
                    item.Status = "Aberto";
                    Status = $"{item.Code} aberto com sucesso.";
                    await Task.Delay(3000);
                }
                catch (Exception ex)
                {
                    item.Status = "Erro";
                    Status = $"{item.Code}: {ex.Message}";
                    break;
                }
            }

            _settingsService.Save(_settings);

            if (items.All(item => item.Status == "Aberto"))
            {
                Status = "Abertura concluída.";
            }
        }
        finally
        {
            _isBusy = false;
            RefreshCommandStates();
        }
    }

    private void RefreshCommandStates()
    {
        SelectAllCommand.NotifyCanExecuteChanged();
        ClearSelectionCommand.NotifyCanExecuteChanged();
        OpenSelectedCommand.NotifyCanExecuteChanged();
        OpenAllCommand.NotifyCanExecuteChanged();
    }

    private static async Task WaitUntilStartedAsync(System.Diagnostics.Process process)
    {
        await Task.Delay(1500);

        if (process.HasExited)
        {
            throw new InvalidOperationException("O processo foi encerrado logo após a inicialização.");
        }
    }
}
