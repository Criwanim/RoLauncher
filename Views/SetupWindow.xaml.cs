using System.Windows;
using RoLauncher.ViewModels;

namespace RoLauncher.Views;

public partial class SetupWindow : Window
{
    public SetupWindow()
    {
        InitializeComponent();
        DataContext = new SetupViewModel();
    }
}
