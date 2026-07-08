using System.Collections.ObjectModel;
using System.Windows.Controls;
using GaussianTool.Objects.Configs;
using GaussianTool.Objects.FileHandling;

namespace GaussianTool.View.UserControls;

public partial class CalcOverviewScrollViewer : UserControl
{
    public ObservableCollection<TreeNode> RootNodes { get; } = [];
    
    public CalcOverviewScrollViewer()
    {
        InitializeComponent();
        
        DataContext = this;
        
        Config cfg = Config.Load();

        TreeNode root = TreeNode.BuildTree(cfg.LocalRechnungenPath);

        foreach (var child in root.Children)
        {
            RootNodes.Add(child);
        }
    }
    
    
    
}