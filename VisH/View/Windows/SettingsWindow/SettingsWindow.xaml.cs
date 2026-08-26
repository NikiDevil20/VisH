using System.Windows;
using VisH.ViewModel;

namespace VisH.View.Windows.SettingsWindow;

public partial class SettingsWindow : Window
{
    public SettingsWindow(string startupMessage = "")
    {
        InitializeComponent();
        DataContext = new SettingsWindowViewModel(startupMessage);
    }
}
