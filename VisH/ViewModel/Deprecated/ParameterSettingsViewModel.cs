using System.Collections.ObjectModel;

namespace VisH.ViewModel;

public class ParameterSettingsViewModel : ViewModelBase
{
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
    
    private string _selectedFunctional;
    private string _selectedBasisSet;

    public string SelectedFunctional
    {
        get => _selectedFunctional;
        set
        {
            _selectedFunctional = value;
            OnPropertyChanged();
        }
    }
    
    public string SelectedBasisSet
    {
        get => _selectedBasisSet;
        set
        {
            _selectedBasisSet = value;
            OnPropertyChanged();
        }
    }

    public ParameterSettingsViewModel()
    {
        SelectedFunctional = Functionals[0];
        SelectedBasisSet = BasisSets[0];
    }
}
