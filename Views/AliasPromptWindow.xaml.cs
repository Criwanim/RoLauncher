using System.Windows;

namespace RoLauncher.Views;

public partial class AliasPromptWindow : Window
{
    public AliasPromptWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => AliasTextBox.Focus();
    }

    public string Alias => AliasTextBox.Text.Trim();

    private void Confirm_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
