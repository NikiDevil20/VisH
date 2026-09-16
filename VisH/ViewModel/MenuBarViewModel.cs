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
        
        List<Calculation> calculationsToSubmit = new();

        foreach (var bundledParameters in allBundledParameters)
        {
            switch (bundledParameters.JobType)
            {
                case JobTypes.GeometryOptimization:
                    calculationsToSubmit.Add(CalculationBuilder.GeometryOptimization(bundledParameters, _sshService));
                    break;
                case JobTypes.TimeDependant:
                    var geometryOptimization = CalculationBuilder.GeometryOptimization(
                        bundledParameters with { JobType = JobTypes.GeometryOptimization },
                        _sshService);

                    var geometryJobIds = _fileHandler.Upload([geometryOptimization]);
                    var geometryJobId = geometryJobIds.FirstOrDefault()
                        ?? throw new InvalidOperationException("Geometry optimization job submission failed.");
                    geometryOptimization.MetaData.JobId = geometryJobId;
                    geometryOptimization.Molecule.DrawSvg(geometryOptimization.Paths.RelativeDirectory.GetPath(), new PythonBridge());
                    geometryOptimization.SaveCalculation();

                    var timeDependentCalculation = CalculationBuilder.TimeDependant(
                        bundledParameters,
                        geometryJobId,
                        geometryOptimization.Paths.ChkFile.GetPath(PathType.Cluster),
                        _sshService);
                    calculationsToSubmit.Add(timeDependentCalculation);
                    break;
                default:
                    throw new NotImplementedException("Unsupported job type");
            }
        }

        if (calculationsToSubmit.Count == 0)
        {
            return;
        }

        var jobIds = _fileHandler.Upload(calculationsToSubmit.ToArray());

        for (int i = 0; i < calculationsToSubmit.Count; i++)
        {
            var calculation = calculationsToSubmit[i];
            calculation.MetaData.JobId = jobIds[i];
            calculation.Molecule.DrawSvg(calculation.Paths.RelativeDirectory.GetPath(), new PythonBridge());
            calculation.SaveCalculation();
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
