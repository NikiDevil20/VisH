using System.Collections.ObjectModel;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Enums;

namespace VisH.ViewModel;

public sealed class WorkflowOptionsViewModel
{
    private static readonly string[] ChargeChoices = ["-3", "-2", "-1", "0", "1", "2", "3"];
    private static readonly int[] MultiplicityChoices = [1, 2, 3, 4];

    private WorkflowOptionsViewModel()
    {
    }

    public ObservableCollection<MoleculeChargeSettingsViewModel> MoleculeStates { get; } = [];
    public ObservableCollection<MethodSelectionViewModel> SharedMethodOptions { get; } = [];
    public ObservableCollection<CalculationOptionsViewModel> Calculations { get; } = [];

    public static WorkflowOptionsViewModel Create(Workflows workflow)
    {
        var options = new WorkflowOptionsViewModel();

        switch (workflow)
        {
            case Workflows.Fluorescence:
                options.MoleculeStates.Add(new MoleculeChargeSettingsViewModel(
                    "Molecule charge and multiplicity",
                    "0",
                    1));
                options.SharedMethodOptions.Add(new MethodSelectionViewModel(
                    "Method used for the workflow"));
                options.Calculations.Add(new CalculationOptionsViewModel(
                    "S1 optimization resources",
                    includesMethodSelection: false,
                    cpus: "16",
                    memoryGb: "64",
                    walltimeHours: "24"));
                options.Calculations.Add(new CalculationOptionsViewModel(
                    "TD calculation resources",
                    includesMethodSelection: false,
                    cpus: "4",
                    memoryGb: "16",
                    walltimeHours: "8"));
                break;

            case Workflows.ElectronAffinity:
                options.MoleculeStates.Add(new MoleculeChargeSettingsViewModel(
                    "Neutral molecule (S0)",
                    "0",
                    1));
                options.MoleculeStates.Add(new MoleculeChargeSettingsViewModel(
                    "Anion",
                    "-1",
                    2));
                options.Calculations.Add(new CalculationOptionsViewModel(
                    "Neutral molecule (S0) calculation",
                    includesMethodSelection: true,
                    cpus: "8",
                    memoryGb: "16",
                    walltimeHours: "24"));
                options.Calculations.Add(new CalculationOptionsViewModel(
                    "Anion calculation",
                    includesMethodSelection: true,
                    cpus: "8",
                    memoryGb: "16",
                    walltimeHours: "24"));
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(workflow), workflow, null);
        }

        return options;
    }

    public static IReadOnlyList<string> Charges => ChargeChoices;
    public static IReadOnlyList<int> Multiplicities => MultiplicityChoices;
    public static IReadOnlyList<Functionals> FunctionalChoices { get; } =
        Enum.GetValues<Functionals>();
    public static IReadOnlyList<BasisSets> BasisSetChoices { get; } =
        Enum.GetValues<BasisSets>();
}

public sealed class MoleculeChargeSettingsViewModel : ViewModelBase
{
    private string _selectedCharge;
    private int _selectedMultiplicity;

    public MoleculeChargeSettingsViewModel(
        string title,
        string selectedCharge,
        int selectedMultiplicity)
    {
        Title = title;
        _selectedCharge = selectedCharge;
        _selectedMultiplicity = selectedMultiplicity;
    }

    public string Title { get; }
    public IReadOnlyList<string> ChargeChoices => WorkflowOptionsViewModel.Charges;
    public IReadOnlyList<int> MultiplicityChoices => WorkflowOptionsViewModel.Multiplicities;

    public string SelectedCharge
    {
        get => _selectedCharge;
        set => SetProperty(ref _selectedCharge, value);
    }

    public int SelectedMultiplicity
    {
        get => _selectedMultiplicity;
        set => SetProperty(ref _selectedMultiplicity, value);
    }
}

public sealed class MethodSelectionViewModel : ViewModelBase
{
    private Functionals _selectedFunctional = VisH.Model.Enums.Functionals.wB97XD;
    private BasisSets _selectedBasisSet = VisH.Model.Enums.BasisSets.def2svp;

    public MethodSelectionViewModel(string title)
    {
        Title = title;
    }

    public string Title { get; }
    public IReadOnlyList<Functionals> Functionals => WorkflowOptionsViewModel.FunctionalChoices;
    public IReadOnlyList<BasisSets> BasisSets => WorkflowOptionsViewModel.BasisSetChoices;

    public Functionals SelectedFunctional
    {
        get => _selectedFunctional;
        set => SetProperty(ref _selectedFunctional, value);
    }

    public BasisSets SelectedBasisSet
    {
        get => _selectedBasisSet;
        set => SetProperty(ref _selectedBasisSet, value);
    }
}

public sealed class CalculationOptionsViewModel : ViewModelBase
{
    private Functionals _selectedFunctional = VisH.Model.Enums.Functionals.wB97XD;
    private BasisSets _selectedBasisSet = VisH.Model.Enums.BasisSets.def2svp;
    private string _cpus;
    private string _memoryGb;
    private string _walltimeHours;

    public CalculationOptionsViewModel(
        string title,
        bool includesMethodSelection,
        string cpus,
        string memoryGb,
        string walltimeHours)
    {
        Title = title;
        IncludesMethodSelection = includesMethodSelection;
        _cpus = cpus;
        _memoryGb = memoryGb;
        _walltimeHours = walltimeHours;
    }

    public string Title { get; }
    public bool IncludesMethodSelection { get; }
    public IReadOnlyList<Functionals> Functionals => WorkflowOptionsViewModel.FunctionalChoices;
    public IReadOnlyList<BasisSets> BasisSets => WorkflowOptionsViewModel.BasisSetChoices;

    public Functionals SelectedFunctional
    {
        get => _selectedFunctional;
        set => SetProperty(ref _selectedFunctional, value);
    }

    public BasisSets SelectedBasisSet
    {
        get => _selectedBasisSet;
        set => SetProperty(ref _selectedBasisSet, value);
    }

    public string Cpus
    {
        get => _cpus;
        set => SetProperty(ref _cpus, value);
    }

    public string MemoryGb
    {
        get => _memoryGb;
        set => SetProperty(ref _memoryGb, value);
    }

    public string WalltimeHours
    {
        get => _walltimeHours;
        set => SetProperty(ref _walltimeHours, value);
    }
}
