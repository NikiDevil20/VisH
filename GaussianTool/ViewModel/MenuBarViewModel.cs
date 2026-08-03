using System.Windows;
using System.Windows.Input;
using GaussianTool.Model;
using GaussianTool.Model.FileHandling;
using GaussianTool.View.Windows;

namespace GaussianTool.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand DebugCommand => new RelayCommand(execute => Debug());
    public RelayCommand NewCalcCommand => new RelayCommand(execute => NewCalc());
    private FileHandler _fileHandler { get; set; }

    public MenuBarViewModel(FileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }


    private void Debug()
    {
        TestGround tg = new TestGround();
    }
    
    
    private void NewCalc()
    {
        var window = new StartNewCalcWindow();
        if (window.ShowDialog() == true)
        {
            var calculation = window.Result;
            string jobId = _fileHandler.Upload(calculation);
            calculation.JobId = jobId;
            calculation.SaveCalculation();
            
            calculation.Molecule.DrawSvg(calculation.GjfPath.WindowsFolder, new PythonBridge());
        }
    }
}