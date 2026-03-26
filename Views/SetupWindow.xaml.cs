using System.Windows;
using System.Windows.Controls;
using RoLauncher.ViewModels;

namespace RoLauncher.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
        DataContext = new SetupViewModel();
    }

    private void NewConfiguration_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is not SetupViewModel viewModel)
        {
            return;
        }

        var alias = ShowAliasPrompt();

        if (alias is null)
        {
            return;
        }

        viewModel.CreateConfigurationWithAlias(alias);
    }

    private string? ShowAliasPrompt()
    {
        var dialog = new Window
        {
            Title = "Alias da conta",
            Owner = this,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            Background = SystemColors.WindowBrush,
            MinWidth = 420
        };

        var root = new Grid
        {
            Margin = new Thickness(18)
        };

        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(10) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        root.RowDefinitions.Add(new RowDefinition { Height = new GridLength(14) });
        root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        var message = new TextBlock
        {
            Text = "Digite o alias da conta que está sendo configurada.\nDeixe em branco para usar o nome padrão.",
            TextWrapping = TextWrapping.Wrap,
            Width = 360
        };
        Grid.SetRow(message, 0);
        root.Children.Add(message);

        var aliasTextBox = new TextBox
        {
            MinWidth = 360
        };
        Grid.SetRow(aliasTextBox, 2);
        root.Children.Add(aliasTextBox);

        var buttonsPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        Grid.SetRow(buttonsPanel, 4);

        var cancelButton = new Button
        {
            Content = "Cancelar",
            MinWidth = 90,
            Margin = new Thickness(0, 0, 8, 0),
            IsCancel = true
        };
        cancelButton.Click += (_, _) => dialog.DialogResult = false;

        var confirmButton = new Button
        {
            Content = "Confirmar",
            MinWidth = 90,
            IsDefault = true
        };
        confirmButton.Click += (_, _) => dialog.DialogResult = true;

        buttonsPanel.Children.Add(cancelButton);
        buttonsPanel.Children.Add(confirmButton);
        root.Children.Add(buttonsPanel);

        dialog.Content = root;
        dialog.Loaded += (_, _) => aliasTextBox.Focus();

        var result = dialog.ShowDialog();
        return result == true ? aliasTextBox.Text : null;
    }
}
