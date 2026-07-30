using System.Windows;
using System.Windows.Controls;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
    }
    
    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is StatusBarViewModel vm)
        {
            vm.Refresh();
        }
    }
}