using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RoLauncher.Services;
using RoLauncher.Models;

namespace RoLauncher.ViewModels;

public partial class LauncherViewModel : ObservableObject
{
    private readonly SettingsService _settingsService = new();
    private readonly TokenService _tokenService = new();
    private readonly GameLaunchService _gameLaunchService = new();
    private readonly AppSettings _settings;

    private bool _isBusy;

    public ObservableCollection<LauncherAccountItemViewModel> Accounts { get; } = new();

    [ObservableProperty]
    private string status = "Pronto";

    public LauncherViewModel()
    {
        _settings = _settingsService.Load();

        foreach (var account in _settings.Accounts.OrderBy(a => a.SlotNumber))
        {
            var item = new LauncherAccountItemViewModel(account);
            item.PropertyChanged += OnAccountItemPropertyChanged;
            Accounts.Add(item);
        }

        Status = Accounts.Count == 0
            ? "Nenhuma conta configurada."
            : $"{Accounts.Count} conta(s) carregada(s).";
    }

    private void OnAccountItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LauncherAccountItemViewModel.IsSelected))
        {
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

    [RelayCommand(CanExecute = nameof(CanSelectAll))]
    private void SelectAll()
    {
        foreach (var item in Accounts)
            item.IsSelected = true;

        Status = "Todas as contas foram marcadas.";
        RefreshCommandStates();
    }

    private bool CanSelectAll()
    {
        return !_isBusy && Accounts.Count > 0;
    }

    [RelayCommand(CanExecute = nameof(CanClearSelection))]
    private void ClearSelection()
    {
        foreach (var item in Accounts)
            item.IsSelected = false;

        Status = "Seleção limpa.";
        RefreshCommandStates();
    }

    private bool CanClearSelection()
    {
        return !_isBusy && Accounts.Any(a => a.IsSelected);
    }

    [RelayCommand(CanExecute = nameof(CanOpenSelected), AllowConcurrentExecutions = false)]
    private async Task OpenSelectedAsync()
    {
        var selected = Accounts
            .Where(a => a.IsSelected)
            .OrderBy(a => a.Account.SlotNumber)
            .ToList();

        await RunLaunchLoopAsync(selected);
    }

    private bool CanOpenSelected()
    {
        return !_isBusy && Accounts.Any(a => a.IsSelected);
    }

    [RelayCommand(CanExecute = nameof(CanOpenAll), AllowConcurrentExecutions = false)]
    private async Task OpenAllAsync()
    {
        foreach (var item in Accounts)
            item.IsSelected = true;

        RefreshCommandStates();

        var all = Accounts
            .OrderBy(a => a.Account.SlotNumber)
            .ToList();

        await RunLaunchLoopAsync(all);
    }

    private bool CanOpenAll()
    {
        return !_isBusy && Accounts.Count > 0;
    }

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

            if (items.All(i => i.Status == "Aberto"))
                Status = "Abertura concluída.";
        }
        finally
        {
            _isBusy = false;
            RefreshCommandStates();
        }
    }

    private static async Task WaitUntilStartedAsync(System.Diagnostics.Process process)
    {
        await Task.Delay(1500);

        if (process.HasExited)
            throw new InvalidOperationException("O processo fechou logo após a inicialização.");
    }
}