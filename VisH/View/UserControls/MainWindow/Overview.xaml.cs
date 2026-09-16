using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using VisH.Model;
using VisH.Model.WPFDisplayObjects;
using VisH.ViewModel;

namespace VisH.View.UserControls.MainWindow;

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
