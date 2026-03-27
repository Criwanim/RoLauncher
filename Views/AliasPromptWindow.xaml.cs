using System.Windows;

namespace RoLauncher.Views;

public partial class AliasPromptWindow : Window
{
    public AliasPromptWindow()
    {
        InitializeComponent();

        Loaded += (_, _) =>
        {
            AliasTextBox.Focus();
            AliasTextBox.SelectAll();
        };
    }

    public string Alias => AliasTextBox.Text.Trim();

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(AliasTextBox.Text))
        {
            MessageBox.Show(
                this,
                "Informe um alias para continuar.",
                "RoLauncher",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            AliasTextBox.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
