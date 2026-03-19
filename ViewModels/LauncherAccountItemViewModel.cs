using CommunityToolkit.Mvvm.ComponentModel;
using RoLauncher.Models;

namespace RoLauncher.ViewModels;

public partial class LauncherAccountItemViewModel : ObservableObject
{
    public AccountProfile Account { get; }

    public string Code => Account.Code;

    public string DisplayName =>
        string.IsNullOrWhiteSpace(Account.DisplayName)
            ? Account.Code
            : Account.DisplayName;

    public string InstanceFolderPath => Account.InstanceFolderPath;

    public string LastLaunchText =>
        Account.LastLaunchAt?.ToString("dd/MM/yyyy HH:mm") ?? "Nunca aberto";

    [ObservableProperty]
    private bool isSelected;

    [ObservableProperty]
    private string status = "Aguardando";

    public LauncherAccountItemViewModel(AccountProfile account)
    {
        Account = account;
    }

    public void Refresh()
    {
        OnPropertyChanged(nameof(LastLaunchText));
    }
}