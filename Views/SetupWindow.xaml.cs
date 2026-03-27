using System.Windows;
using RoLauncher.ViewModels;

namespace RoLauncher.Views;

public partial class SetupWindow : Window
{
    private SetupViewModel ViewModel => (SetupViewModel)DataContext;

    public SetupWindow()
    {
        InitializeComponent();
        DataContext = new SetupViewModel();
    }

    private async void NewConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (!ViewModel.CanStartNewConfiguration())
        {
            MessageBox.Show(
                this,
                "Preencha os caminhos antes de criar uma nova configuração.",
                "RoLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var aliasWindow = new AliasPromptWindow
        {
            Owner = this
        };

        var confirmed = aliasWindow.ShowDialog();
        if (confirmed != true)
        {
            return;
        }

        await ViewModel.CreateConfigurationWithAliasAsync(aliasWindow.Alias);
    }

    private void DeleteSelectedAccount_Click(object sender, RoutedEventArgs e)
    {
        if (ViewModel.SelectedAccount is null)
        {
            MessageBox.Show(
                this,
                "Selecione uma conta para deletar.",
                "RoLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
            return;
        }

        var account = ViewModel.SelectedAccount;

        if (account.SlotNumber == 1 || string.Equals(account.Code, "ro_win1", System.StringComparison.OrdinalIgnoreCase))
        {
            MessageBox.Show(
                this,
                "A conta ro_win1 não pode ser excluída porque ela é a instância base usada para criar novas contas.",
                "RoLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Deseja realmente deletar a conta '{account.DisplayName}'?\n\n" +
            "Isso irá remover:\n" +
            "- os arquivos da instância no diretório do jogo\n" +
            "- os arquivos de backup de token\n" +
            "- o atalho da área de trabalho, se existir",
            "Confirmar exclusão",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        ViewModel.DeleteSelectedAccountCommand.Execute(null);
    }
}
