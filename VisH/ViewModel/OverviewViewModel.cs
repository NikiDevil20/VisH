using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Text.Json;
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
    public ObservableCollection<DataGridItem> MethodGridItems { get; } = [];
    private bool _isSpectrumVisible;
    public bool IsSpectrumVisible
    {
        get => _isSpectrumVisible;
        private set => SetProperty(ref _isSpectrumVisible, value);
    }
    public double[] SpectrumWavelengths { get; private set; } = [];
    public double[] SpectrumIntensities { get; private set; } = [];
    public string SpectrumType { get; private set; } = string.Empty;
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
    private bool _isMoleculeViewerVisible;
    public bool IsMoleculeViewerVisible
    {
        get => _isMoleculeViewerVisible;
        private set => SetProperty(ref _isMoleculeViewerVisible, value);
    }
    private string _moleculeViewerMessage = string.Empty;
    public string MoleculeViewerMessage
    {
        get => _moleculeViewerMessage;
        private set => SetProperty(ref _moleculeViewerMessage, value);
    }
    public string MoleculeViewerPayloadJson { get; private set; } = string.Empty;
    public string MoleculeViewerOrbitalMode { get; private set; } = "HOMO";
    public bool HasHomoCube { get; private set; }
    public bool HasLumoCube { get; private set; }

    public void SetMoleculeViewerError(string message)
    {
        MoleculeViewerMessage = message;
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
                ClearProperties();
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
        MethodGridItems.Clear();
        SpectrumWavelengths = [];
        SpectrumIntensities = [];
        SpectrumType = string.Empty;
        IsSpectrumVisible = false;
        IsMoleculeViewerVisible = false;
        MoleculeViewerMessage = string.Empty;
        MoleculeViewerPayloadJson = string.Empty;
        MoleculeViewerOrbitalMode = "HOMO";
        HasHomoCube = false;
        HasLumoCube = false;
        OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
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

        DictToGridItems(SetupMetaData(calculation.MetaData, calculation.GaussianParameters), MetaDataGridItems);
        DictToGridItems(SetupEnergies(calculation.Results), EnergiesGridItems);
        DictToGridItems(SetupFrequencies(calculation.Results), FrequenciesGridItems);
        DictToGridItems(SetupOrbitals(calculation.Results), OrbitalsGridItems);
        DictToGridItems(SetupMethod(calculation.GaussianParameters), MethodGridItems);
        SetSpectrum(calculation);
        SetMoleculeViewer(calculation);
        HasValidSelection = true;
    }

    private void SetMoleculeViewer(Calculation calculation)
    {
        IsMoleculeViewerVisible = false;
        MoleculeViewerMessage = string.Empty;
        MoleculeViewerPayloadJson = string.Empty;
        MoleculeViewerOrbitalMode = "HOMO";
        HasHomoCube = false;
        HasLumoCube = false;

        if (calculation.GaussianParameters?.Jobtype != JobTypes.GeometryOptimization)
        {
            OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
            return;
        }

        var geometry = calculation.Results?.CoordResults;
        if (string.IsNullOrWhiteSpace(geometry))
        {
            MoleculeViewerMessage = "Keine finale Geometrie für diese Rechnung verfügbar.";
            IsMoleculeViewerVisible = true;
            OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
            return;
        }

        var geometryResult = MoleculeDataValidation.ParseGeometry(geometry);
        if (!geometryResult.IsValid)
        {
            MoleculeViewerMessage = calculation.Results?.GeometryMessage
                ?? geometryResult.Message;
            IsMoleculeViewerVisible = true;
            OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
            return;
        }

        var calculationDirectory = SelectedPath?.GetPath(PathType.Local);
        if (string.IsNullOrWhiteSpace(calculationDirectory))
        {
            MoleculeViewerMessage = "Das Rechnungsverzeichnis ist nicht verfügbar.";
            IsMoleculeViewerVisible = true;
            OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
            return;
        }

        var cubeErrors = new List<string>();
        string[] files;
        try
        {
            files = Directory.EnumerateFiles(calculationDirectory, "*", SearchOption.TopDirectoryOnly)
                .Where(path => string.Equals(Path.GetExtension(path), ".cube", StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            files = [];
            cubeErrors.Add($"Cube-Dateien konnten nicht durchsucht werden: {exception.Message}");
        }
        var xyzGeometry = ToXyz(geometryResult.Atoms);
        var optimizationComplete = IsOptimizationComplete(calculation);
        var topologyMessage = MoleculeDataValidation.HasSameAtomOrder(
            geometryResult.Atoms,
            calculation.Molecule?.GetAtomElements())
            ? string.Empty
            : "Geometrie angezeigt; Bindungstopologie konnte nicht sicher der Molekülstruktur zugeordnet werden.";
        var homoCube = optimizationComplete
            ? FindValidatedCube(files, "homo.cube", geometryResult.Atoms, cubeErrors)
            : null;
        var lumoCube = optimizationComplete
            ? FindValidatedCube(files, "lumo.cube", geometryResult.Atoms, cubeErrors)
            : null;
        var densityCube = optimizationComplete
            ? FindValidatedCube(files, "density.cube", geometryResult.Atoms, cubeErrors)
            : null;
        HasHomoCube = homoCube is not null;
        HasLumoCube = lumoCube is not null;
        MoleculeViewerOrbitalMode = HasHomoCube ? "HOMO" : HasLumoCube ? "LUMO" : "HOMO";
        var statusMessage = string.Join(" ", new[]
        {
            optimizationComplete ? string.Empty :
                "Letzte bekannte Geometrie; Optimierung nicht als vollständig bestätigt.",
            topologyMessage,
            string.Join(" ", cubeErrors)
        }.Where(message => !string.IsNullOrWhiteSpace(message)));
        MoleculeViewerMessage = statusMessage;
        var payload = new
        {
            geometry = xyzGeometry,
            statusMessage,
            homoCube,
            lumoCube,
            densityCube,
            orbitalMode = MoleculeViewerOrbitalMode,
            isoValue = 0.02,
            opacity = 0.65
        };
        MoleculeViewerPayloadJson = JsonSerializer.Serialize(payload);
        IsMoleculeViewerVisible = true;
        OnPropertyChanged(nameof(MoleculeViewerPayloadJson));
    }

    private static string ToXyz(IReadOnlyList<GeometryAtom> atoms)
    {
        var lines = atoms.Select(atom =>
            $"{atom.Element} {atom.X.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            $"{atom.Y.ToString(System.Globalization.CultureInfo.InvariantCulture)} " +
            atom.Z.ToString(System.Globalization.CultureInfo.InvariantCulture));
        return $"{atoms.Count}{Environment.NewLine}Optimized geometry{Environment.NewLine}" +
               string.Join(Environment.NewLine, lines);
    }

    private static bool IsOptimizationComplete(Calculation calculation)
    {
        if (calculation.Results?.NormalTermination == true &&
            calculation.Results.OptimizationConverged == true)
        {
            return true;
        }

        return false;
    }

    private static string? FindValidatedCube(
        IReadOnlyList<string> files,
        string expectedName,
        IReadOnlyList<GeometryAtom> geometry,
        ICollection<string> errors)
    {
        var matches = files
            .Where(path => string.Equals(
                Path.GetFileName(path),
                expectedName,
                StringComparison.OrdinalIgnoreCase))
            .ToArray();
        if (matches.Length == 0)
        {
            return null;
        }
        if (matches.Length > 1)
        {
            errors.Add($"Cube-Datei {expectedName} ist mehrdeutig und wurde nicht geladen.");
            return null;
        }

        var validation = MoleculeDataValidation.ValidateCube(matches[0], geometry);
        if (!validation.IsValid)
        {
            errors.Add($"{expectedName}: {validation.Message}");
            return null;
        }

        var relativePath = Path.GetRelativePath(
            Config.Load().LocalRechnungenPath,
            matches[0]).Replace('\\', '/');
        return $"https://molecule.local/{relativePath}";
    }

    private void SetSpectrum(Calculation calculation)
    {
        var peaks = calculation.Results?.Peaks;
        if (calculation.GaussianParameters?.Jobtype != JobTypes.TimeDependant ||
            peaks is null || peaks.Count == 0)
        {
            SpectrumWavelengths = [];
            SpectrumIntensities = [];
            SpectrumType = string.Empty;
            IsSpectrumVisible = false;
            return;
        }

        var minimum = peaks.Min(peak => peak.Wavelength) - 5;
        var maximum = peaks.Max(peak => peak.Wavelength) + 25;
        var wavelengths = new SortedSet<double>();
        for (var wavelength = minimum; wavelength <= maximum; wavelength += 0.1)
        {
            wavelengths.Add(wavelength);
        }

        // foreach (var peak in peaks)
        // {
        //     wavelengths.Add(peak.Wavelength);
        // }

        var peakWavelengths = peaks.Select(peak => peak.Wavelength).ToArray();
        var oscillatorStrengths = peaks.Select(peak => peak.OscillatorStrength).ToArray();
        var spectrum = new SpectrumCalculator().CalculateSpectrum(
            wavelengths.ToArray(), peakWavelengths, oscillatorStrengths);

        SpectrumWavelengths = spectrum.Keys.ToArray();
        SpectrumIntensities = spectrum.Values.ToArray();
        SpectrumType = calculation.Molecule?.State == State.S0 ? "Absorption" : "Emission";
        IsSpectrumVisible = true;
        OnPropertyChanged(nameof(SpectrumWavelengths));
        OnPropertyChanged(nameof(SpectrumIntensities));
        OnPropertyChanged(nameof(SpectrumType));
    }


    private void DictToGridItems(Dictionary<string, string> dict, ObservableCollection<DataGridItem> targetCollection)
    {
        targetCollection.Clear();
        
        foreach (var ele in dict)
        {
            targetCollection.Add(new DataGridItem(ele.Key, ele.Value));
        }
    }
    private Dictionary<string, string> SetupMetaData(MetaData? metaData, GaussianParameters? gaussianParameters)
    {
        var dict = new Dictionary<string, string>();
        if (metaData == null) return dict;
        dict["JobName"] = metaData.JobName ?? "";
        dict["JobId"] = metaData.JobId ?? "";
        dict["Submitted"] = FormatDate(metaData.Dates.SubmittedDate);
        dict["Started"] = FormatDate(metaData.Dates.StartDate);
        dict["Ended"] = FormatDate(metaData.Dates.EndDate);
        dict["Walltime"] = metaData.Ressources.Walltime.ToString();
        dict["CPU Usage"] = FormatCpuUsage(
            metaData.Ressources.UsedCpu,
            gaussianParameters?.NProc ?? metaData.Ressources.UsedCores);
        dict["Memory Usage"] = FormatMemoryUsage(
            metaData.Ressources.UsedMemory,
            gaussianParameters?.Memory ?? 0);
        return dict;
    }

    private static string FormatDate(DateTime date)
    {
        return date == default
            ? ""
            : date.ToString("g", CultureInfo.CurrentCulture);
    }

    private static string FormatCpuUsage(float usedCpu, int requestedCores)
    {
        if (usedCpu <= 0 || requestedCores <= 0)
        {
            return "";
        }

        var percentage = usedCpu / (requestedCores * 100f) * 100f;
        return $"{percentage.ToString("F1", CultureInfo.CurrentCulture)} % of {requestedCores} cores";
    }

    private static string FormatMemoryUsage(float usedMemoryKb, int requestedMemoryGb)
    {
        if (usedMemoryKb <= 0 || requestedMemoryGb <= 0)
        {
            return "";
        }

        var requestedMemoryKb = requestedMemoryGb * 1024f * 1024f;
        var percentage = usedMemoryKb / requestedMemoryKb * 100f;
        return $"{percentage.ToString("F1", CultureInfo.CurrentCulture)} % of {requestedMemoryGb} GB";
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
        if (results?.NHomo is not int homoIndex ||
            homoIndex < 0 ||
            results.MoEnergies.Length <= homoIndex + 1)
        {
            return dict;
        }

        dict["HOMO"] = results.MoEnergies[homoIndex].ToString("F6");
        dict["LUMO"] = results.MoEnergies[homoIndex + 1].ToString("F6");
        dict["Gap"] = (results.MoEnergies[homoIndex + 1] - results.MoEnergies[homoIndex]).ToString("F6");
        return dict;
    }

    private static Dictionary<string, string> SetupMethod(GaussianParameters? gaussianParameters)
    {
        var dict = new Dictionary<string, string>();
        if (gaussianParameters == null) return dict;

        dict["Functional"] = gaussianParameters.Functional.ToString();
        dict["Basis set"] = BasisSetsParser.ToString(gaussianParameters.BasisSet);
        dict["Keywords"] = gaussianParameters.Keywords ?? "";
        dict["Link keywords"] = gaussianParameters.LinkKeywords ?? "";
        return dict;
    }
    
    
}
