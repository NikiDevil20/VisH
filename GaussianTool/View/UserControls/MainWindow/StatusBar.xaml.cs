using System.Windows.Controls;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class StatusBar : UserControl
{
    public StatusBar()
    {
        InitializeComponent();
        DataContext = new StatusBarViewModel();
    }
}