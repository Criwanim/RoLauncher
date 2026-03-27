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
}
