using System.Collections.ObjectModel;
using System.Windows;
using VisH.Model;
using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Enums;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.Parameters;

namespace VisH.ViewModel;

public class NewCalcViewModel : ViewModelBase
{
    
    
    public ObservableCollection<string> Charges { get; } =
    [
        "-3",
        "-2",
        "-1",
        "0",
        "1",
        "2",
        "3"
    ];
    
    public State[] StateCollection => Enum.GetValues<State>();
    public Solvents[] SolventCollection => Enum.GetValues<Solvents>();
    public BasisSets[] BasisSetCollection => Enum.GetValues<BasisSets>();
    public Functionals[] FunctionalCollection => Enum.GetValues<Functionals>();
    public JobTypes[] CalcTypeCollection => Enum.GetValues<JobTypes>();

    private State? _selectedState;
    private string? _selectedCharge;
    private Functionals? _selectedFunctional;
    private BasisSets? _selectedBasisSet;
    private string? _moleculeName;
    private string? _smilesString;
    private string? _jobId;
    private string? _nCores;
    private string? _ram;
    private JobTypes? _selectedCalcType;
    private Solvents? _selectedSolvent;
    private string? _optionalKeywords;
    private string? _scanContext;
    private bool? _dispersionCorrection;
    private Queues? _selectedQueue;

    public State? SelectedState
    {
        get => _selectedState;
        set
        {
            _selectedState = value;
            OnPropertyChanged();
        }
    }
    public string? SelectedCharge
    {
        get => _selectedCharge;
        set
        {
            _selectedCharge = value;
            OnPropertyChanged();
        }
    }
    public Functionals? SelectedFunctional
    {
        get => _selectedFunctional;
        set
        {
            _selectedFunctional = value;
            OnPropertyChanged();
        }
    }
    public BasisSets? SelectedBasisSet
    {
        get => _selectedBasisSet;
        set
        {
            _selectedBasisSet = value;
            OnPropertyChanged();
        }
    }
    public string? MoleculeName
    {
        get => _moleculeName;
        set
        {
            _moleculeName = value;
            OnPropertyChanged();
        }
    }
    public string? SmilesString
    {
        get => _smilesString;
        set
        {
            _smilesString = value;
            OnPropertyChanged();
        }
    }
    public string? JobId
    {
        get => _jobId;
        set
        {
            _jobId = value;
            OnPropertyChanged();
        }
    }
    public string? NCores
    {
        get => _nCores;
        set
        {
            _nCores = value;
            OnPropertyChanged();
        }
    }
    public string? Ram
    {
        get => _ram;
        set
        {
            _ram = value;
            OnPropertyChanged();
        }
    }
    public JobTypes? SelectedCalcType
    {
        get => _selectedCalcType;
        set
        {
            _selectedCalcType = value;
            OnPropertyChanged();
        }
    }
    public Solvents? SelectedSolvent
    {
        get => _selectedSolvent;
        set
        {
            _selectedSolvent = value;
            OnPropertyChanged();
        }
    }
    public string? OptionalKeywords
    {
        get => _optionalKeywords;
        set
        {
            _optionalKeywords = value;
            OnPropertyChanged();
        }
    }
    public string? ScanContext
    {
        get => _scanContext;
        set
        {
            _scanContext = value;
            OnPropertyChanged();
        }
    }
    public bool? DispersionCorrection
    {
        get => _dispersionCorrection;
        set
        {
            _dispersionCorrection = value;
            OnPropertyChanged();
        }
    }
    public Queues? SelectedQueue
    {
        get => _selectedQueue;
        set
        {
            _selectedQueue = value;
            OnPropertyChanged();
        }
    }

    public RelayCommand SaveTemplateCommand => new RelayCommand(
        execute=>SaveTemplate(), canExecute=> CanSaveTemplate());
    public RelayCommand RunCommand => new RelayCommand(
        execute=>Run(), canExecute=> CanRun());
    public RelayCommand CancelCommand => new RelayCommand(
        execute=>Cancel());
    
    public event Action<bool>? RequestClose; 
    
    public BundledConstructionParameters[]? Result { get; private set; }
    
    public NewCalcViewModel()
    {
        SelectedState = State.S0;
        SelectedCharge = Charges[3];
        SelectedFunctional = Functionals.wB97XD;
        SelectedBasisSet = BasisSets.def2svp;
        SelectedCalcType = JobTypes.GeometryOptimization;
        SelectedSolvent = Solvents.Dichloromethane;
        SelectedQueue = Queues.WorkQueue;
    }
    
    private BundledConstructionParameters BuildBundledParameters()
    {
        return new BundledConstructionParameters(
            MoleculeName: MoleculeName ?? throw new ArgumentNullException(nameof(MoleculeName)),
            SmilesString: SmilesString ?? throw new ArgumentNullException(nameof(SmilesString)),
            Charge: SelectedCharge ?? "0",
            State: SelectedState ?? State.S0,
            Memory: int.Parse(Ram ?? "8"),
            NProcs: int.Parse(NCores ?? "2"),
            Functional: SelectedFunctional ?? Functionals.wB97XD,
            BasisSet: SelectedBasisSet ?? BasisSets.def2svp,
            JobType: SelectedCalcType ?? JobTypes.GeometryOptimization,
            Solvent: SelectedSolvent ?? Solvents.None,
            MaxWalltime: SelectedQueue switch
            {
                Queues.WorkQueue => TimeSpan.FromHours(71),
                Queues.LongQueue => TimeSpan.FromDays(5),
                _ => TimeSpan.FromHours(72)
            },
            GeometryOptimizationJobId: JobId,
            OptionalKeywords: OptionalKeywords,
            DispersionCorrection: DispersionCorrection,
            ScanContext: ScanContext
        );
    }

    private void SaveTemplate()
    {
        // Implementation for saving template
        
        RequestClose?.Invoke(true);
    }

    private void Run()
    {
        if (!AllEntriesValid())
        {
            return;
        }
        
        BundledConstructionParameters bundledParams = BuildBundledParameters();

        Result = [bundledParams];
        
        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        var answer = MessageBox.Show(
            "Are you sure you want to cancel? \nUnsaved changes will be lost.",
            "Confirm Cancel", MessageBoxButton.YesNo, MessageBoxImage.Question);
        
        if (answer == MessageBoxResult.Yes)
        {
            RequestClose?.Invoke(false);
        }
    }
    
    private bool CanSaveTemplate()
    {
        return AllEntriesFilled();
    }
    
    private bool CanRun()
    {
        return AllEntriesFilled();
    }
    
    private bool AllEntriesFilled()
    {
        return !string.IsNullOrEmpty(MoleculeName) &&
               !string.IsNullOrEmpty(SmilesString) &&
               !string.IsNullOrEmpty(NCores) &&
               !string.IsNullOrEmpty(Ram);
    }
    
    private bool AllEntriesValid()
    {
        if (!Molecule.IsValidSmiles(SmilesString))
        {
            MessageBox.Show(
                "Please enter a valid SMILES string.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return false;
        }
        
        if (!int.TryParse(NCores, out int cores) || cores <= 0)
        {
            MessageBox.Show(
                "Please enter a valid number of cores.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return false;
        }
        
        if (!int.TryParse(Ram, out int ram) || ram <= 0)
        {
            MessageBox.Show(
                "Please enter a valid amount of RAM.",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
            return false;
        }

        return true;
    }
    
}
