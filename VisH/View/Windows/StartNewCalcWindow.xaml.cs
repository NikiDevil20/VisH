using System.Windows;
using VisH.Model;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.PostRun;
using VisH.ViewModel;

namespace VisH.View.Windows;

public partial class StartNewCalcWindow : Window
{
    public BundledConstructionParameters[]? Result =>
        ((NewCalcViewModel)DataContext).Result;
    
    public StartNewCalcWindow() : this(null)
    {
    }

    public StartNewCalcWindow(JobFinder? jobFinder)
    {
        InitializeComponent();
        var vm = new NewCalcViewModel(jobFinder);
        DataContext = vm;
        vm.RequestClose += result =>
        {
            DialogResult = result;
        };
    }
}
