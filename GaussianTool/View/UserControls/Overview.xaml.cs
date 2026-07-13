using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class Overview : UserControl
{
    
    public Overview()
    {
        InitializeComponent();
        
        DataContext = new OverviewViewModel();
        
        
    }
    
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var viewModel = DataContext as OverviewViewModel;
        viewModel?.SelectedNode = e.NewValue as TreeNode;
    }
    
}