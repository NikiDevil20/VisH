using System.Globalization;
using System.Runtime.CompilerServices;
using System.Windows;
using VisH.Model.CalculationProperties;

namespace VisH.ViewModel;

public sealed record SetupConfiguration(
    string MoleculeName,
    string GeometrySource,
    string? Smiles,
    string? GeometryJobId,
    string WallTimeProfile,
    TimeSpan WallTime,
    int Memory,
    int CpuCount,
    string JobType,
    string ElectronicState,
    string MethodLevel,
    string? Functional,
    string BasisSet,
    bool AddPolarizationFunctions,
    bool AddDiffuseFunctions,
    int Charge,
    string SpinMultiplicity,
    string SolventModel,
    string? Solvent);

public sealed class SetupViewModel : ViewModelBase
{
    private string _moleculeName = string.Empty;
    private string _geometrySource = "SMILES";
    private string _smiles = string.Empty;
    private string _geometryJobId = string.Empty;
    private string _wallTimeProfile = "Work";
    private string _customWallTime = string.Empty;
    private string _memory = "8";
    private string _cpuCount = "2";
    private string _jobType = "Opt + Freq";
    private string _electronicState = "Ground State";
    private string _methodLevel = "DFT";
    private string _functional = "B3LYP";
    private string _basisSet = "def2-SVP";
    private bool _addPolarizationFunctions;
    private bool _addDiffuseFunctions;
    private string _charge = "0";
    private string _spinMultiplicity = "Singlet";
    private string _solventModel = "None";
    private string _solvent = "Water";

    public SetupViewModel()
    {
        UpdateBasisSetOptions();
        UpdatePreview();
    }

    public string MoleculeName { get => _moleculeName; set => SetAndRefresh(ref _moleculeName, value); }
    public string GeometrySource { get => _geometrySource; set => SetAndRefresh(ref _geometrySource, value); }
    public string Smiles { get => _smiles; set => SetAndRefresh(ref _smiles, value); }
    public string GeometryJobId { get => _geometryJobId; set => SetAndRefresh(ref _geometryJobId, value); }
    public string WallTimeProfile { get => _wallTimeProfile; set => SetAndRefresh(ref _wallTimeProfile, value); }
    public string CustomWallTime { get => _customWallTime; set => SetAndRefresh(ref _customWallTime, value); }
    public string Memory { get => _memory; set => SetAndRefresh(ref _memory, value); }
    public string CpuCount { get => _cpuCount; set => SetAndRefresh(ref _cpuCount, value); }
    public string JobType { get => _jobType; set => SetAndRefresh(ref _jobType, value); }
    public string ElectronicState { get => _electronicState; set => SetAndRefresh(ref _electronicState, value); }
    public string MethodLevel { get => _methodLevel; set => SetAndRefresh(ref _methodLevel, value); }
    public string Functional { get => _functional; set => SetAndRefresh(ref _functional, value); }
    public string BasisSet
    {
        get => _basisSet;
        set
        {
            if (!SetProperty(ref _basisSet, value))
                return;

            UpdateBasisSetOptions();
            UpdatePreview();
        }
    }
    public bool AddPolarizationFunctions
    {
        get => _addPolarizationFunctions;
        set => SetProperty(ref _addPolarizationFunctions, value);
    }
    public bool AddDiffuseFunctions
    {
        get => _addDiffuseFunctions;
        set => SetProperty(ref _addDiffuseFunctions, value);
    }
    public string Charge { get => _charge; set => SetAndRefresh(ref _charge, value); }
    public string SpinMultiplicity { get => _spinMultiplicity; set => SetAndRefresh(ref _spinMultiplicity, value); }
    public string SolventModel { get => _solventModel; set => SetAndRefresh(ref _solventModel, value); }
    public string Solvent { get => _solvent; set => SetAndRefresh(ref _solvent, value); }

    public bool CanAddPolarizationFunctions { get; private set; }
    public bool CanAddDiffuseFunctions { get; private set; }
    public string GaussianHeaderPreview { get; private set; } = string.Empty;

    public SetupConfiguration? Result { get; private set; }
    public event Action<bool>? RequestClose;

    public RelayCommand RunCommand => new(_ => Run());
    public RelayCommand CancelCommand => new(_ => RequestClose?.Invoke(false));

    private void SetAndRefresh<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (!SetProperty(ref field, value, propertyName))
            return;

        UpdatePreview();
    }

    private void UpdateBasisSetOptions()
    {
        CanAddPolarizationFunctions = _basisSet is "STO-3G" or "3-21G" or "6-31G" or "6-31G(d)" or
            "6-31G(d,p)" or "6-31+G(d,p)" or "6-311G(d,p)";
        CanAddDiffuseFunctions = _basisSet is "6-31G" or "6-31G(d)" or "6-31G(d,p)" or
            "6-31+G(d,p)" or "6-311G(d,p)";

        if (!CanAddPolarizationFunctions)
            AddPolarizationFunctions = false;
        if (!CanAddDiffuseFunctions)
            AddDiffuseFunctions = false;

        OnPropertyChanged(nameof(CanAddPolarizationFunctions));
        OnPropertyChanged(nameof(CanAddDiffuseFunctions));
    }

    private void UpdatePreview()
    {
        var functional = MethodLevel == "DFT" ? Functional : "Hartree-Fock";
        var solvent = SolventModel == "None" ? string.Empty : $" scrf=({SolventModel.ToLowerInvariant()},solvent={Solvent})";
        GaussianHeaderPreview =
            $"%NProcShared={CpuCount}\n" +
            $"%mem={Memory}GB\n" +
            $"#p {functional} {BasisSet}{solvent}\n\n" +
            $"{MoleculeName}\n\n" +
            $"{Charge} {(SpinMultiplicity == "Triplet" ? "3" : "1")}";
        OnPropertyChanged(nameof(GaussianHeaderPreview));
    }

    private void Run()
    {
        if (!TryBuildResult(out var result))
            return;

        Result = result;
        RequestClose?.Invoke(true);
    }

    private bool TryBuildResult(out SetupConfiguration? result)
    {
        result = null;

        if (string.IsNullOrWhiteSpace(MoleculeName))
            return ShowValidationError("Please enter a molecule name.");

        if (GeometrySource == "SMILES")
        {
            if (string.IsNullOrWhiteSpace(Smiles))
                return ShowValidationError("Please enter a SMILES string.");
            if (!Molecule.IsValidSmiles(Smiles))
                return ShowValidationError("Please enter a valid SMILES string.");
        }
        else if (GeometrySource == "Job")
        {
            if (string.IsNullOrWhiteSpace(GeometryJobId))
                return ShowValidationError("Please enter a geometry job ID.");
        }
        else
        {
            return ShowValidationError("Please select a geometry source.");
        }

        if (!int.TryParse(CpuCount, NumberStyles.Integer, CultureInfo.InvariantCulture, out var cpuCount) || cpuCount <= 0)
            return ShowValidationError("Please enter a positive number of CPUs.");
        if (!int.TryParse(Memory, NumberStyles.Integer, CultureInfo.InvariantCulture, out var memory) || memory <= 0)
            return ShowValidationError("Please enter a positive amount of RAM.");
        if (!int.TryParse(Charge, NumberStyles.Integer, CultureInfo.InvariantCulture, out var charge))
            return ShowValidationError("Please enter a valid integer charge.");

        var wallTime = WallTimeProfile switch
        {
            "Work" => TimeSpan.FromHours(71),
            "Long" => TimeSpan.FromDays(5),
            "Custom" when TimeSpan.TryParse(CustomWallTime, CultureInfo.InvariantCulture, out var custom) && custom > TimeSpan.Zero => custom,
            "Custom" => TimeSpan.Zero,
            _ => TimeSpan.Zero
        };
        if (wallTime <= TimeSpan.Zero)
            return ShowValidationError("Please enter a valid custom walltime.");
        if (MethodLevel == "DFT" && string.IsNullOrWhiteSpace(Functional))
            return ShowValidationError("Please select a functional.");
        if (string.IsNullOrWhiteSpace(BasisSet) || string.IsNullOrWhiteSpace(SpinMultiplicity))
            return ShowValidationError("Please complete the method settings.");
        if (SolventModel != "None" && string.IsNullOrWhiteSpace(Solvent))
            return ShowValidationError("Please select a solvent.");

        result = new SetupConfiguration(
            MoleculeName, GeometrySource, GeometrySource == "SMILES" ? Smiles : null,
            GeometrySource == "Job" ? GeometryJobId : null, WallTimeProfile, wallTime,
            memory, cpuCount, JobType, ElectronicState, MethodLevel,
            MethodLevel == "DFT" ? Functional : null, BasisSet,
            AddPolarizationFunctions, AddDiffuseFunctions, charge, SpinMultiplicity,
            SolventModel, SolventModel == "None" ? null : Solvent);
        return true;
    }

    private static bool ShowValidationError(string message)
    {
        MessageBox.Show(message, "Invalid setup", MessageBoxButton.OK, MessageBoxImage.Error);
        return false;
    }
}
