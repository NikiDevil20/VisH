using System.Collections.ObjectModel;
using System.Windows;
using VisH.Model;
using VisH.Model.Enums;
using VisH.Model.FileHandling;
using VisH.Model.Parameters;
using VisH.Model.Runner;
using VisH.Model.Setup;

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
    
    State[] StateCollection => Enum.GetValues<State>();
    Solvents[] SolventCollection => Enum.GetValues<Solvents>();
    BasisSets[] BasisSetCollection => Enum.GetValues<BasisSets>();
    Functionals[] FunctionalCollection => Enum.GetValues<Functionals>();
    CalculationType[] CalcTypeCollection => Enum.GetValues<CalculationType>();

    private State? _selectedState;
    private string? _selectedCharge;
    private Functionals? _selectedFunctional;
    private BasisSets? _selectedBasisSet;
    private string? _moleculeName;
    private string? _smilesString;
    private string? _jobId;
    private string? _nCores;
    private string? _ram;
    private CalculationType? _selectedCalcType;
    private Solvents? _selectedSolvent;
    
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
    public CalculationType? SelectedCalcType
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

    public RelayCommand SaveTemplateCommand => new RelayCommand(
        execute=>SaveTemplate(), canExecute=> CanSaveTemplate());
    public RelayCommand RunCommand => new RelayCommand(
        execute=>Run(), canExecute=> CanRun());
    public RelayCommand CancelCommand => new RelayCommand(
        execute=>Cancel());
    
    public event Action<bool>? RequestClose; 
    
    public NewCalcViewModel()
    {
        Console.WriteLine("NewCalcViewModel initialized");
        SelectedState = State.S0;
        SelectedCharge = Charges[3];
        SelectedFunctional = Functionals.wb97xd;
        SelectedBasisSet = BasisSets.def2svp;
        SelectedCalcType = CalculationType.GeometryOptimization;
        SelectedSolvent = Solvents.Dichloromethane;
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
        
        CalcParameters calcParam = BuildParameter();
        Molecule mol = new Molecule(SmilesString, MoleculeName, SelectedCharge, SelectedState.Multiplicity);
        
       
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

    private CalcParameters BuildParameter()
    {
        var keywords = KeywordSelector.GetKeywords(
            SelectedSolvent,
            SelectedCalcType,
            SelectedState
        );

        var calcParam = new CalcParameters(
            calcType: SelectedCalcType,
            proc: NCores,
            ram: Ram,
            functional: SelectedFunctional,
            basisSet: SelectedBasisSet,
            state: SelectedState?.Name,
            solvent: SelectedSolvent,
            time: "70:99:99",
            keywords: keywordsAndLink[0]
        );
        
        var link = new CalcParameters(
            calcType: SelectedCalcType,
            proc: NCores,
            ram: Ram,
            functional: SelectedFunctional,
            basisSet: SelectedBasisSet,
            state: SelectedState?.Name,
            solvent: SelectedSolvent,
            time: "71:99:99",
            keywords: keywordsAndLink[1],
            isLink: true
        );
        calcParam.Link = link;

        return calcParam;
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
