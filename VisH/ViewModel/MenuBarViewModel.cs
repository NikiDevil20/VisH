using System.Windows;
using System.Windows.Input;
using VisH.Model;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.View.Windows;

namespace VisH.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand NewCalcCommand => new RelayCommand(execute => NewCalc());
    private FileHandler _fileHandler;

    public MenuBarViewModel(FileHandler fileHandler)
    {
        _fileHandler = fileHandler;
    }
    
    private void NewCalc()
    {
        var window = new StartNewCalcWindow();
        if (window.ShowDialog() == true)
        {
            BundledConstructionParameters[] allBundledParameters = window.Result;
            
            List<Calculation> calculations = new List<Calculation>();

            foreach (var bundledParameters in allBundledParameters)
            {
                var calculation = CalculationBuilder.Build(bundledParameters);
                calculations.Add(calculation);
            }
            
            var jobIds = _fileHandler.Upload(calculations.ToArray());

            for (int i = 0; i < calculations.Count; i++)
            {
                calculations[i].MetaData.JobId = jobIds[i];
                calculations[i].Molecule.DrawSvg(calculations[i].Paths.RelativeDirectory.GetPath(), new PythonBridge());
                calculations[i].SaveCalculation();
            }
        }
    }
}
