using System.Collections.ObjectModel;
using VisH.Model;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;
using VisH.Model.WPFDisplayObjects;

namespace VisH.ViewModel;

public class OverviewViewModel : ViewModelBase
{
    private LogFileAnalyzer _logFileAnalyzer;
    private FileHandler _fileHandler;
    private readonly SshService _sshService;
    private readonly JobManager _jobManager;
    public ObservableCollection<TreeNode> RootNodes { get; } = [];
    // public ObservableCollection<DataGridItem> Properties { get; } = [];
    
    public ObservableCollection<DataGridItem> MetaDataGridItems { get; } = [];
    public ObservableCollection<DataGridItem> EnergiesGridItems { get; } = [];
    public ObservableCollection<DataGridItem> FrequenciesGridItems { get; } = [];
    public ObservableCollection<DataGridItem> OrbitalsGridItems{ get; } = [];
    private string _moleculeImage = "";
    public string MoleculeImage
    {
        get => _moleculeImage;
        set => SetProperty(ref _moleculeImage, value);
    }
    private string _moleculeName = "";
    public string MoleculeName
    {
        get => _moleculeName;
        set => SetProperty(ref _moleculeName, value);
    }
    private PathObject? SelectedPath { get; set; }
    private TreeNode? _selectedNode;
    public TreeNode? SelectedNode 
    { 
        get => _selectedNode;
        set
        {
            _selectedNode = value;
            if (value is null)
            {
                SelectedPath = null;
                OnPropertyChanged();
                return;
            }
            
            SelectedPath = new PathObject(value.FullPath, sshService: _sshService, jobManager: _jobManager);
            RefreshSelection();
            OnPropertyChanged();
        }
    }
    
    public OverviewViewModel(
        LogFileAnalyzer logFileAnalyzer,
        FileHandler fileHandler,
        SshService sshService,
        JobManager jobManager)
    {
        _logFileAnalyzer = logFileAnalyzer;
        _fileHandler = fileHandler;
        _sshService = sshService;
        _jobManager = jobManager;
        SetupTreeview();
        _fileHandler.ClusterChanged += () => SetupTreeview();
    }

    private void RefreshSelection()
    {
        DisplayProperties();
    }

    private bool StructurePresent()
    {
        if (SelectedPath is null)
            return false;

        if (SelectedPath.TryGetFileWithEnding(".png") is null)
        {
            return false;
        }
        return true;
    }

    private bool PropertiesPresent()
    {
        if (SelectedPath is null)
            return false;
        
        if (SelectedPath.TryGetFileWithEnding(".log") is null || SelectedPath.TryGetFileWithEnding(".lg") is null)
        {
            Console.WriteLine(".log and .lg files not present.");
            return false;
        }

        return true;
    }
    
    private void SetMoleculeImage(PathObject directory)
    {
        if (StructurePresent())
        {
            var pngFile = directory.TryGetFileWithEnding(".png");
            MoleculeImage = pngFile.WindowsPath;
        }
        else
        {
            MoleculeImage = "/VisH;component/Assets/FileNotFound.png";
        }
    }

    private void ClearProperties()
    {
        MetaDataGridItems.Clear();
        EnergiesGridItems.Clear();
        FrequenciesGridItems.Clear();
        OrbitalsGridItems.Clear();
    }

    private void SetupTreeview()
    {
        var cfg = Config.Load();

        TreeNode root = TreeNode.BuildTree(cfg.LocalRechnungenPath);

        RootNodes.Clear();
        foreach (var child in root.Children)
        {
            RootNodes.Add(child);
        }
    }
    
    private void DisplayProperties()
    {
        if (SelectedPath is null)
            return;
        
        SetMoleculeImage(SelectedPath);
        
        if (!PropertiesPresent())
        {
            ClearProperties();
            return;
        }

        // CalcResults calcResults = _logFileAnalyzer.Run(SelectedPath);
        //
        // MoleculeName = calcResults.MetaData.JobName;
        //
        // DictToGridItems(SetupMetaData(calcResults.MetaData), MetaDataGridItems);
        // DictToGridItems(SetupEnergies(calcResults.Energy), EnergiesGridItems);
        // DictToGridItems(SetupFrequencies(calcResults.Frequency), FrequenciesGridItems);
        // DictToGridItems(SetupOrbitals(calcResults.Orbitals), OrbitalsGridItems);
    }


    private void DictToGridItems(Dictionary<string, string> dict, ObservableCollection<DataGridItem> targetCollection)
    {
        targetCollection.Clear();
        
        foreach (var ele in dict)
        {
            targetCollection.Add(new DataGridItem(ele.Key, ele.Value));
        }
    }
    // private Dictionary<string, string> SetupMetaData(MetaData metaData)
    // {
    //     return metaData.ToDictionary();
    // }
    //
    // private Dictionary<string, string> SetupEnergies(EnergyResults energyResults)
    // {
    //     return energyResults.ToDictionary();
    // }
    //
    // private Dictionary<string, string> SetupFrequencies(Frequency frequency)
    // {
    //     return frequency.ToDictionary();
    // }
    //
    // private Dictionary<string, string> SetupOrbitals(Orbitals orbitals)
    // {
    //     return orbitals.ToDictionary();
    //
    // }
    
    
}
