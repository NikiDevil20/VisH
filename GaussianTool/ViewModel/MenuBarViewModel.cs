using System.Windows;
using System.Windows.Input;
using GaussianTool.Model;
using GaussianTool.View.Windows;

namespace GaussianTool.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand DebugCommand => new RelayCommand(execute => Debug());
    public RelayCommand NewCalcCommand => new RelayCommand(execute => NewCalc());

    public MenuBarViewModel()
    {
    }


    private void Debug()
    {
        TestGround tg = new TestGround();
    }
    
    
    private void NewCalc()
    {
        var window = new StartNewCalcWindow();
        window.ShowDialog();
    }
}