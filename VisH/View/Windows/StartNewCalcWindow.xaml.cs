using System.Windows;
using VisH.Model;
using VisH.ViewModel;

namespace VisH.View.Windows;

public partial class StartNewCalcWindow : Window
{
    public Calculation? Result =>
        ((NewCalcViewModel)DataContext).Result;
    
    public StartNewCalcWindow()
    {
        InitializeComponent();
        var vm = new NewCalcViewModel();
        DataContext = vm;
        vm.RequestClose += result =>
        {
            DialogResult = result;
        };
    }
}
