using System.Windows;
using GaussianTool.ViewModel;

namespace GaussianTool.View.Windows;

public partial class StartNewCalcWindow : Window
{
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