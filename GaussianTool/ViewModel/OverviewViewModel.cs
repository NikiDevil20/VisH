using System.Collections.ObjectModel;
using GaussianTool.Model;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;

namespace GaussianTool.ViewModel;

public class OverviewViewModel : ViewModelBase
{
    
    public ObservableCollection<TreeNode> RootNodes { get; } = [];
    public ObservableCollection<DataGridItem> Properties { get; set; }
    public string MoleculeImage { get; set; }
    public string MoleculeName { get; set; }
    private TreeNode? _selectedNode;
    public TreeNode? SelectedNode 
    { 
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            OnPropertyChanged();
            
            // Console.WriteLine(value.FullPath);
        }
    }
    
    public OverviewViewModel()
    {
        SetupTreeview();
        SetMoleculeImage();
        DisplayProperties();
        
        
        Console.WriteLine(_selectedNode);
    }
    
    public void SetMoleculeImage()
    {
        MoleculeName = "Benzene";
        MoleculeImage = @"C:\\Users\\nikla\\RiderProjects\\GaussianGUI\\GaussianTool\\Assets\\Benzene_200.svg.png";
    }

    public void SetupTreeview()
    {
        Config cfg = Config.Load();

        TreeNode root = TreeNode.BuildTree(cfg.LocalRechnungenPath);

        foreach (var child in root.Children)
        {
            RootNodes.Add(child);
        }
    }
    
    public void DisplayProperties()
    {
        string[] values = { "100", "120", "140", "160" };
        string[] keywords = { "Property1", "Property2", "Property3", "Property4" };
        
        Dictionary<string, string> propDict = new Dictionary<string, string>();
        for (int i = 0; i < values.Length; i++)
        {
            propDict.Add(keywords[i], values[i]);
        }
        
        
        Properties = new ObservableCollection<DataGridItem>();


        foreach (var ele in propDict)
        {
            DataGridItem Item = new DataGridItem(ele.Key, ele.Value);
            Properties.Add(Item);
        }
        
        
    }
}