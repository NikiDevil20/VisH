using System.Collections.ObjectModel;
using System.Windows.Controls;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class OverviewTreeview : UserControl
{
    
    public OverviewTreeview()
    {
        InitializeComponent();
        
        DataContext = new OverviewTreeviewViewModel();
        
        
    }
    
    
    
}