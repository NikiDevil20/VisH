using System.Collections.ObjectModel;
using VisH.Model.Parameters;

namespace VisH.ViewModel;

public class MoleculeSettingsViewModel : ViewModelBase
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
    
    private States? _selectedState;
    
    public States? SelectedState
    {
        get => _selectedState;
        set
        {
            _selectedState = value;
            OnPropertyChanged();
        }
    }

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
    
    private string? _selectedCharge;
    
    public string? SelectedCharge
    {
        get => _selectedCharge;
        set
        {
            _selectedCharge = value;
            OnPropertyChanged();
        }
    }
   
    

    public MoleculeSettingsViewModel()
    {
        SelectedState = States[0];
        SelectedCharge = Charges[3];
        
    }
}
