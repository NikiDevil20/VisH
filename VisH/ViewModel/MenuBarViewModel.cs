using System.Windows;
using System.Windows.Input;
using Accessibility;
using VisH.Model;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;
using VisH.View.Windows;
using VisH.View.Windows.SettingsWindow;

namespace VisH.ViewModel;

public class MenuBarViewModel : ViewModelBase
{
    public RelayCommand NewCalcCommand => new RelayCommand(execute => NewCalc());
    public RelayCommand SettingsCommand => new RelayCommand(_ => OpenSettings());
    private FileHandler _fileHandler;
    private readonly SshService _sshService;
    private readonly JobFinder _jobFinder;

    public MenuBarViewModel(FileHandler fileHandler, SshService sshService, JobFinder jobFinder)
    {
        _fileHandler = fileHandler;
        _sshService = sshService;
        _jobFinder = jobFinder;
    }
    
    private void NewCalc()
    {
        var window = new StartNewCalcWindow();
        if (window.ShowDialog() == false)
        {
            return;
        }
        
        BundledConstructionParameters[] allBundledParameters = window.Result;
        
        List<Calculation> calculations = new List<Calculation>();

        foreach (var bundledParameters in allBundledParameters)
        {
            switch (bundledParameters.JobType)
            {
                case JobTypes.GeometryOptimization:
                    var calculation = CalculationBuilder.GeometryOptimization(bundledParameters, _sshService);
                    calculations.Add(calculation);
                    break;
                case JobTypes.TimeDependant:
                    var timeDependentCalculation = CalculationBuilder.TimeDependant(
                        bundledParameters, bundledParameters.GeometryOptimizationJobId, _sshService, _jobFinder);
                    calculations.Add(timeDependentCalculation);
                    break;
                default:
                    throw new NotImplementedException("Unsupported job type");
            }
        }
        
        var jobIds = _fileHandler.Upload(calculations.ToArray());

        for (int i = 0; i < calculations.Count; i++)
        {
            calculations[i].MetaData.JobId = jobIds[i];
            calculations[i].Molecule.DrawSvg(calculations[i].Paths.RelativeDirectory.GetPath(), new PythonBridge());
            calculations[i].SaveCalculation();
        }
        
    }

    private void OpenSettings()
    {
        SettingsWindow window = new()
        {
            Owner = Application.Current?.MainWindow
        };
        window.ShowDialog();
    }
}
