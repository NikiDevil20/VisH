using System.Collections.ObjectModel;
using System.IO;
using VisH.Model;
using VisH.Model.CalculationProperties;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.CalculationObject;
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
    private bool _hasValidSelection;
    public bool HasValidSelection
    {
        get => _hasValidSelection;
        set => SetProperty(ref _hasValidSelection, value);
    }
    private DirectoryExtension? SelectedPath { get; set; }
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
            
            var relativePath = new DirectoryExtension("", _sshService).GetRelativePath(value.FullPath, PathType.Local);
            SelectedPath = new DirectoryExtension(relativePath, _sshService);
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

    private FileExtension? TryGetFileWithEnding(DirectoryExtension? directory, string ending)
    {
        if (directory is null)
            return null;

        var fullPath = directory.GetPath(PathType.Local);
        if (!Directory.Exists(fullPath))
            return null;

        return directory.GetContent(PathType.Local)
            .OfType<FileExtension>()
            .FirstOrDefault(f => f.GetFileName(PathType.Local).EndsWith(ending, StringComparison.OrdinalIgnoreCase));
    }

    private bool StructurePresent()
    {
        if (SelectedPath is null)
            return false;

        if (TryGetFileWithEnding(SelectedPath, ".png") is null)
        {
            return false;
        }
        return true;
    }

    private bool PropertiesPresent()
    {
        if (SelectedPath is null)
            return false;
        
        if (TryGetFileWithEnding(SelectedPath, ".log") is null || TryGetFileWithEnding(SelectedPath, ".lg") is null)
        {
            return false;
        }

        return true;
    }
    
    private void SetMoleculeImage(DirectoryExtension directory)
    {
        if (StructurePresent())
        {
            var pngFile = TryGetFileWithEnding(directory, ".png");
            MoleculeImage = pngFile?.GetPath(PathType.Local) ?? "/VisH;component/Assets/FileNotFound.png";
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
        {
            HasValidSelection = false;
            return;
        }
        
        SetMoleculeImage(SelectedPath);
        
        if (!PropertiesPresent())
        {
            ClearProperties();
            HasValidSelection = false;
            return;
        }

        var calculationJson = TryGetFileWithEnding(SelectedPath, ".json");
        if (calculationJson is null)
        {
            ClearProperties();
            HasValidSelection = false;
            return;
        }

        var calculation = Calculation.FromJson(calculationJson.GetPath(PathType.Local), _sshService);
        MoleculeName = calculation.MetaData?.JobName ?? "";

        DictToGridItems(SetupMetaData(calculation.MetaData), MetaDataGridItems);
        DictToGridItems(SetupEnergies(calculation.Results), EnergiesGridItems);
        DictToGridItems(SetupFrequencies(calculation.Results), FrequenciesGridItems);
        DictToGridItems(SetupOrbitals(calculation.Results), OrbitalsGridItems);
        HasValidSelection = true;
    }


    private void DictToGridItems(Dictionary<string, string> dict, ObservableCollection<DataGridItem> targetCollection)
    {
        targetCollection.Clear();
        
        foreach (var ele in dict)
        {
            targetCollection.Add(new DataGridItem(ele.Key, ele.Value));
        }
    }
    private Dictionary<string, string> SetupMetaData(MetaData? metaData)
    {
        var dict = new Dictionary<string, string>();
        if (metaData == null) return dict;
        dict["JobName"] = metaData.JobName ?? "";
        dict["JobId"] = metaData.JobId ?? "";
        dict["Walltime"] = metaData.Ressources.Walltime.ToString();
        dict["UsedCpu"] = metaData.Ressources.UsedCpu.ToString();
        dict["UsedMemory"] = metaData.Ressources.UsedMemory.ToString();
        return dict;
    }

    private Dictionary<string, string> SetupEnergies(Results? results)
    {
        var dict = new Dictionary<string, string>();
        if (results == null || results.ScfEnergies.Length == 0) return dict;
        dict["Total Energy"] = results.ScfEnergies[^1].ToString("F6");
        return dict;
    }

    private Dictionary<string, string> SetupFrequencies(Results? results)
    {
        var dict = new Dictionary<string, string>();
        if (results == null || results.AllFreqs.Length == 0) return dict;
        dict["NImag"] = results.AllFreqs.Count(f => f < 0).ToString();
        dict["Lowest"] = results.AllFreqs.Min().ToString("F2");
        dict["Highest"] = results.AllFreqs.Max().ToString("F2");
        return dict;
    }

    private Dictionary<string, string> SetupOrbitals(Results? results)
    {
        var dict = new Dictionary<string, string>();
        if (results == null || results.MoEnergies.Length <= results.NHomo + 1) return dict;
        dict["HOMO"] = results.MoEnergies[results.NHomo].ToString("F6");
        dict["LUMO"] = results.MoEnergies[results.NHomo + 1].ToString("F6");
        dict["Gap"] = (results.MoEnergies[results.NHomo + 1] - results.MoEnergies[results.NHomo]).ToString("F6");
        return dict;
    }
    
    
}
