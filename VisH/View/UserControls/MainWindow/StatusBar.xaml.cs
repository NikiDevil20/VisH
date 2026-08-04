using System.Windows;
using System.Windows.Controls;
using VisH.ViewModel;

namespace VisH.View.UserControls;

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
            vm.RefreshJobList();
        }
    }
}
