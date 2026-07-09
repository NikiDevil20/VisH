using System.Windows;
using System.Windows.Input;
using GaussianTool.Model;

namespace GaussianTool.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand DebugCommand => new RelayCommand(execute => Debug());

    public MenuBarViewModel()
    {
    }


    private void Debug()
    {
        TestGround tg = new TestGround();
    }
}