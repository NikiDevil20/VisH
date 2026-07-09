using System.Collections.ObjectModel;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;

namespace GaussianTool.ViewModel;

public class OverviewTreeviewViewModel
{
    
    public ObservableCollection<TreeNode> RootNodes { get; } = [];
    
    public OverviewTreeviewViewModel()
    {

        Config cfg = Config.Load();

        TreeNode root = TreeNode.BuildTree(cfg.LocalRechnungenPath);

        foreach (var child in root.Children)
        {
            RootNodes.Add(child);
        }
    }
}