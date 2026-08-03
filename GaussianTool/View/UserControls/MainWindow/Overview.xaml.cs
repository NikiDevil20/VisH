using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using GaussianTool.Model;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;
using GaussianTool.ViewModel;

namespace GaussianTool.View.UserControls;

public partial class Overview : UserControl
{
    
    public Overview()
    {
        InitializeComponent();

        // PythonBridge pythonBridge = new PythonBridge();
        // LogFileAnalyzer logFileAnalyzer = new LogFileAnalyzer(pythonBridge);
        //
        // DataContext = new OverviewViewModel(logFileAnalyzer);
        
    }
    
    private void TreeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
        var viewModel = DataContext as OverviewViewModel;
        viewModel?.SelectedNode = e.NewValue as TreeNode;
    }
    
}