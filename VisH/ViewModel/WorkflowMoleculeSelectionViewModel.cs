using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationProperties;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.Enums;
using VisH.Model.PostRun;

namespace VisH.ViewModel;

public sealed class WorkflowMoleculeSelectionViewModel : ViewModelBase
{
    private readonly JobFinder? _jobFinder;
    private readonly PythonBridge _pythonBridge;
    private string _moleculeName = string.Empty;
    private string _smilesString = string.Empty;
    private string? _errorMessage;
    private bool _existingMoleculesLoaded;

    public WorkflowMoleculeSelectionViewModel(JobFinder? jobFinder = null)
    {
        _jobFinder = jobFinder;
        _pythonBridge = new PythonBridge();
        AddMoleculeCommand = new RelayCommand(
            _ => AddMolecule(),
            _ => !string.IsNullOrWhiteSpace(MoleculeName) &&
                 !string.IsNullOrWhiteSpace(SmilesString));

    }

    public ObservableCollection<WorkflowMoleculeViewModel> Molecules { get; } = [];
    public RelayCommand AddMoleculeCommand { get; }

    public string MoleculeName
    {
        get => _moleculeName;
        set
        {
            if (SetProperty(ref _moleculeName, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string SmilesString
    {
        get => _smilesString;
        set
        {
            if (SetProperty(ref _smilesString, value))
                CommandManager.InvalidateRequerySuggested();
        }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    public IReadOnlyList<WorkflowGeometrySelection> SelectedGeometries =>
        Molecules.SelectMany(molecule => molecule.GetSelectedGeometries()).ToArray();

    public void LoadExistingMolecules()
    {
        if (_existingMoleculesLoaded)
            return;
        _existingMoleculesLoaded = true;

        if (_jobFinder is null)
            return;

        try
        {
            var geometryJobs = _jobFinder.GetSuccessfulLocalGeometryCalculations();
            var groupedJobs = geometryJobs
                .GroupBy(calculation => new
                {
                    calculation.Molecule!.MoleculeName,
                    calculation.Molecule.SmilesString
                })
                .OrderBy(group => group.Key.MoleculeName, StringComparer.OrdinalIgnoreCase);

            foreach (var group in groupedJobs)
            {
                var jobs = group
                    .OrderBy(calculation => calculation.MetaData!.JobId)
                    .Select(calculation => new WorkflowGeometryViewModel(calculation))
                    .ToArray();
                string? previewPath = null;
                try
                {
                    previewPath = CreatePreview(group.Key.SmilesString);
                }
                catch (Exception exception) when (
                    exception is IOException or InvalidOperationException or UnauthorizedAccessException or
                        System.ComponentModel.Win32Exception)
                {
                    ErrorMessage = $"Could not draw a preview for '{group.Key.MoleculeName}': {exception.Message}";
                }

                Molecules.Add(new WorkflowMoleculeViewModel(
                    group.Key.MoleculeName,
                    group.Key.SmilesString,
                    previewPath,
                    jobs));
                Molecules[^1].SelectionChanged += OnMoleculeSelectionChanged;
            }
        }
        catch (Exception exception) when (
            exception is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            ErrorMessage = $"Could not load existing geometry jobs: {exception.Message}";
        }
    }

    private void AddMolecule()
    {
        ErrorMessage = null;
        var moleculeName = MoleculeName.Trim();
        var smiles = SmilesString.Trim();

        if (Molecules.Any(molecule =>
                string.Equals(molecule.Name, moleculeName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(molecule.SmilesString, smiles, StringComparison.Ordinal)))
        {
            ErrorMessage = "This molecule has already been added.";
            return;
        }

        try
        {
            if (!Molecule.IsValidSmiles(smiles))
            {
                ErrorMessage = "The SMILES string is invalid.";
                return;
            }

            var previewPath = CreatePreview(smiles);
            var molecule = new WorkflowMoleculeViewModel(moleculeName, smiles, previewPath);
            molecule.SelectionChanged += OnMoleculeSelectionChanged;
            Molecules.Add(molecule);
            MoleculeName = string.Empty;
            SmilesString = string.Empty;
        }
        catch (Exception exception) when (
            exception is IOException or InvalidOperationException or UnauthorizedAccessException or
                System.ComponentModel.Win32Exception)
        {
            ErrorMessage = $"Could not draw the molecule structure: {exception.Message}";
        }
    }

    private string CreatePreview(string smiles)
    {
        var previewDirectory = Path.Combine(
            Config.UserDataDirectory,
            "WorkflowMoleculePreviews",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(previewDirectory);
        var result = _pythonBridge.ExecuteScriptWithStatus(
            "DrawPngFromSmiles.py",
            [smiles, previewDirectory]);
        if (result.exitCode != 0)
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(result.stderr)
                    ? $"Preview script exited with code {result.exitCode}."
                    : result.stderr.Trim());

        var imagePath = Path.Combine(previewDirectory, "structure.png");
        if (!File.Exists(imagePath))
            throw new FileNotFoundException(
                "The molecule preview script did not create an image.",
                imagePath);

        return imagePath;
    }

    private void OnMoleculeSelectionChanged()
    {
        OnPropertyChanged(nameof(SelectedGeometries));
        CommandManager.InvalidateRequerySuggested();
    }
}

public sealed class WorkflowMoleculeViewModel : ViewModelBase
{
    private bool _isNewGeometrySelected;

    public WorkflowMoleculeViewModel(
        string name,
        string smilesString,
        string? previewPath,
        IEnumerable<WorkflowGeometryViewModel>? existingGeometries = null)
    {
        Name = name;
        SmilesString = smilesString;
        PreviewPath = previewPath;
        ExistingGeometries = new ObservableCollection<WorkflowGeometryViewModel>(
            existingGeometries ?? []);
        foreach (var geometry in ExistingGeometries)
            geometry.SelectionChanged = OnExistingGeometrySelectionChanged;
    }

    public string Name { get; }
    public string SmilesString { get; }
    public string? PreviewPath { get; }
    public ObservableCollection<WorkflowGeometryViewModel> ExistingGeometries { get; }
    public bool IsNewGeometrySelected
    {
        get => _isNewGeometrySelected;
        set
        {
            if (!SetProperty(ref _isNewGeometrySelected, value))
                return;
            if (value)
            {
                foreach (var geometry in ExistingGeometries)
                    geometry.IsSelected = false;
            }
            SelectionChanged?.Invoke();
        }
    }

    public event Action? SelectionChanged;

    public BitmapImage? PreviewImage
    {
        get
        {
            if (string.IsNullOrWhiteSpace(PreviewPath) || !File.Exists(PreviewPath))
                return null;

            var image = new BitmapImage();
            image.BeginInit();
            image.UriSource = new Uri(PreviewPath, UriKind.Absolute);
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.EndInit();
            image.Freeze();
            return image;
        }
    }

    public void SelectNewGeometry(bool selected)
    {
        IsNewGeometrySelected = selected;
    }

    public IEnumerable<WorkflowGeometrySelection> GetSelectedGeometries()
    {
        if (IsNewGeometrySelected)
        {
            yield return new WorkflowGeometrySelection(Name, SmilesString, null);
            yield break;
        }

        foreach (var geometry in ExistingGeometries.Where(geometry => geometry.IsSelected))
        {
            yield return new WorkflowGeometrySelection(
                Name,
                SmilesString,
                geometry.Calculation);
        }
    }

    private void OnExistingGeometrySelectionChanged(
        WorkflowGeometryViewModel changedGeometry,
        bool selected)
    {
        if (selected)
            IsNewGeometrySelected = false;
        SelectionChanged?.Invoke();
    }
}

public sealed class WorkflowGeometryViewModel : ViewModelBase
{
    private bool _isSelected;

    public WorkflowGeometryViewModel(Calculation calculation)
    {
        Calculation = calculation;
    }

    public Calculation Calculation { get; }
    public string JobId => Calculation.MetaData!.JobId!;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (!SetProperty(ref _isSelected, value))
                return;
            SelectionChanged?.Invoke(this, value);
        }
    }

    internal Action<WorkflowGeometryViewModel, bool>? SelectionChanged { get; set; }
}

public sealed record WorkflowGeometrySelection(
    string MoleculeName,
    string SmilesString,
    Calculation? ExistingCalculation);
