using System.Windows;
using System.Windows.Input;
using VisH.Model;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.View.Windows;

namespace VisH.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand NewCalcCommand => new RelayCommand(execute => NewCalc());
    private FileHandler _fileHandler { get; set; }

    public MenuBarViewModel(FileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }
    
    
    private void NewCalc()
    {
        var window = new StartNewCalcWindow();
        if (window.ShowDialog() == true)
        {
            var bundledParameters = window.Result;
            
            
            
            // string jobId = _fileHandler.Upload(calculation);
            // calculation.JobId = jobId;
            // calculation.SaveCalculation();
            //
            // calculation.Molecule.DrawSvg(calculation.GjfPath.WindowsFolder, new PythonBridge());
        }
    }
}
