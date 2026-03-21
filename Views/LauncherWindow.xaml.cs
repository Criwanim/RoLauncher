using System.Windows;
using RoLauncher.ViewModels;

namespace RoLauncher.Views;

public partial class LauncherWindow : Window
{
    public LauncherWindow()
    {
        InitializeComponent();
        DataContext = new LauncherViewModel();
    }
}
