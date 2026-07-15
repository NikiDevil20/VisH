using System.Collections.ObjectModel;
using System.Windows;
using GaussianTool.Model.Parameters;

namespace GaussianTool.ViewModel;

public class NewCalcViewModel : ViewModelBase
{
    public ObservableCollection<States> States { get; } = 
    [
        new States("0", "S"),
        new States("1", "S"),
        new States("2", "S"),
        new States("3", "S"),
        new States("1", "T"),
        new States("2", "T"),
        new States("3", "T")
    ];
    
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
    
    public ObservableCollection<string> Functionals { get; } =
    [
        "wb97xd",
        "B3LYP",
        "PBE0"
    ];

    public ObservableCollection<string> BasisSets { get; } =
    [
        "def2svp",
        "def2tzvp",
        "def2qzvp"
    ];
    
    private States? _selectedState;
    private string? _selectedCharge;
    private string? _selectedFunctional;
    private string? _selectedBasisSet;
    
    public States? SelectedState
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
    
    public string? SelectedFunctional
    {
        get => _selectedFunctional;
        set
        {
            _selectedFunctional = value;
            OnPropertyChanged();
        }
    }
    
    public string? SelectedBasisSet
    {
        get => _selectedBasisSet;
        set
        {
            _selectedBasisSet = value;
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
        SelectedState = States[0];
        SelectedCharge = Charges[3];
        SelectedFunctional = Functionals[0];
        SelectedBasisSet = BasisSets[0];
    }
    
    public string CollectEntries()
    {
        return $"{SelectedState?.Name ?? "N/A"} {SelectedCharge ?? "N/A"} " +
               $"{SelectedFunctional ?? "N/A"} {SelectedBasisSet ?? "N/A"}";
    }
    
    private void SaveTemplate()
    {
        // Implementation for saving template
        
        RequestClose?.Invoke(true);
    }

    private void Run()
    {
        // Run!
        string entries = CollectEntries();
        Console.WriteLine(entries);
        
        RequestClose?.Invoke(true);
    }

    private void Cancel()
    {
        var answer = MessageBox.Show(
            "Are you sure you want to cancel?", "Confirm Cancel", MessageBoxButton.YesNo,
            MessageBoxImage.Question);
        
        if (answer == MessageBoxResult.Yes)
        {
            RequestClose?.Invoke(false);
        }
    }
    
    private bool CanSaveTemplate()
    {
        return true;
    }
    
    private bool CanRun()
    {
        return true;
    }
}