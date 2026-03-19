using System.Windows;
using RoLauncher.Views;

namespace RoLauncher;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void OpenSetup_Click(object sender, RoutedEventArgs e)
    {
        var window = new SetupWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }

    private void OpenLauncher_Click(object sender, RoutedEventArgs e)
    {
        var window = new LauncherWindow
        {
            Owner = this
        };

        window.ShowDialog();
    }
}