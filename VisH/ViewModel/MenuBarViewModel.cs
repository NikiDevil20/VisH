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
        var window = new StartNewCalcWindow(_jobFinder);
        if (window.ShowDialog() == false)
        {
            return;
        }
        
        BundledConstructionParameters[] allBundledParameters = window.Result;
        
        List<Calculation> calculationsToSubmit = new();
        var existingCheckpointSources = new Dictionary<Calculation, FileExtension>();
        BundledConstructionParameters? eddBundledParameters = null;

        foreach (var bundledParameters in allBundledParameters)
        {
            switch (bundledParameters.JobType)
            {
                case JobTypes.GeometryOptimization:
                    calculationsToSubmit.Add(CalculationBuilder.GeometryOptimization(bundledParameters, _sshService));
                    break;
                case JobTypes.TimeDependant:
                    if (!string.IsNullOrWhiteSpace(bundledParameters.GeometryOptimizationJobId))
                    {
                        var sourceCalculation = _jobFinder.GetCalculationByJobId(
                            bundledParameters.GeometryOptimizationJobId);

                        if (sourceCalculation?.Paths?.ChkFile == null)
                        {
                            throw new InvalidOperationException(
                                $"The geometry optimization calculation for Job ID " +
                                $"'{bundledParameters.GeometryOptimizationJobId}' could not be loaded.");
                        }

                        calculationsToSubmit.Add(CalculationBuilder.TimeDependant(
                            bundledParameters,
                            bundledParameters.GeometryOptimizationJobId,
                            sourceCalculation.Paths.ChkFile.GetPath(PathType.Cluster),
                            _sshService,
                            useExistingCheckpoint: true));
                        existingCheckpointSources[calculationsToSubmit[^1]] =
                            sourceCalculation.Paths.ChkFile;
                    }
                    else
                    {
                        var geometryOptimization = CalculationBuilder.GeometryOptimization(
                            bundledParameters with { JobType = JobTypes.GeometryOptimization },
                            _sshService);

                        var geometryJobIds = _fileHandler.Upload([geometryOptimization]);
                        var geometryJobId = geometryJobIds.FirstOrDefault()
                            ?? throw new InvalidOperationException("Geometry optimization job submission failed.");
                        geometryOptimization.MetaData.JobId = geometryJobId;
                        DrawMoleculePreview(geometryOptimization);
                        geometryOptimization.SaveCalculation();

                        calculationsToSubmit.Add(CalculationBuilder.TimeDependant(
                            bundledParameters,
                            geometryJobId,
                            geometryOptimization.Paths.ChkFile.GetPath(PathType.Cluster),
                            _sshService));
                    }
                    break;
                case JobTypes.SinglePoint:
                    var singlePointSource = _jobFinder.GetCalculationByJobId(
                        bundledParameters.GeometryOptimizationJobId
                        ?? throw new InvalidOperationException(
                            "A geometry optimization job id is required for a single-point calculation."));

                    if (singlePointSource?.GaussianParameters?.Jobtype != JobTypes.GeometryOptimization ||
                        singlePointSource.Paths?.ChkFile == null)
                    {
                        throw new InvalidOperationException(
                            $"The geometry optimization calculation for Job ID " +
                            $"'{bundledParameters.GeometryOptimizationJobId}' could not be loaded.");
                    }

                    var singlePoint = CalculationBuilder.SinglePoint(
                        bundledParameters,
                        bundledParameters.GeometryOptimizationJobId!,
                        _sshService);
                    calculationsToSubmit.Add(singlePoint);
                    existingCheckpointSources[singlePoint] = singlePointSource.Paths.ChkFile;
                    break;
                case JobTypes.ElectronDensityDifference:
                    eddBundledParameters = bundledParameters;
                    Calculation eddSource;
                    bool sourceIsAlreadyComplete;
                    if (!string.IsNullOrWhiteSpace(bundledParameters.GeometryOptimizationJobId))
                    {
                        eddSource = _jobFinder.GetCalculationByJobId(
                            bundledParameters.GeometryOptimizationJobId)
                            ?? throw new InvalidOperationException(
                                $"The optimization for Job ID '{bundledParameters.GeometryOptimizationJobId}' could not be loaded.");
                        sourceIsAlreadyComplete = true;
                    }
                    else
                    {
                        var optimization = CalculationBuilder.GeometryOptimization(
                            bundledParameters with { JobType = JobTypes.GeometryOptimization },
                            _sshService);
                        var optimizationJobId = _fileHandler.Upload([optimization]).FirstOrDefault()
                            ?? throw new InvalidOperationException("Geometry optimization job submission failed.");
                        optimization.MetaData!.JobId = optimizationJobId;
                        DrawMoleculePreview(optimization);
                        optimization.SaveCalculation();
                        eddSource = optimization;
                        sourceIsAlreadyComplete = false;
                    }

                    if (eddSource.GaussianParameters?.Jobtype != JobTypes.GeometryOptimization ||
                        eddSource.Paths?.ChkFile == null ||
                        eddSource.MetaData?.JobId == null)
                    {
                        throw new InvalidOperationException(
                            "The EDD source must be a geometry optimization with a checkpoint and job id.");
                    }

                    var eddCalculations = CalculationBuilder.ElectronDensityDifference(
                        bundledParameters,
                        eddSource,
                        _sshService,
                        sourceIsAlreadyComplete);
                    calculationsToSubmit.Add(eddCalculations.TimeDependent);
                    calculationsToSubmit.Add(eddCalculations.GroundState);
                    if (sourceIsAlreadyComplete)
                    {
                        existingCheckpointSources[eddCalculations.TimeDependent] = eddSource.Paths.ChkFile;
                        existingCheckpointSources[eddCalculations.GroundState] = eddSource.Paths.ChkFile;
                    }
                    break;
                default:
                    throw new NotImplementedException("Unsupported job type");
            }
        }

        if (calculationsToSubmit.Count == 0)
        {
            return;
        }

        var jobIds = _fileHandler.Upload(
            calculationsToSubmit.ToArray(),
            calculation =>
            {
                if (!existingCheckpointSources.TryGetValue(calculation, out var source))
                {
                    return;
                }

                _fileHandler.CopyCheckpoint(source, calculation.Paths!.ChkFile!);
            });

        for (int i = 0; i < calculationsToSubmit.Count; i++)
        {
            var calculation = calculationsToSubmit[i];
            calculation.MetaData.JobId = jobIds[i];
            DrawMoleculePreview(calculation);
            calculation.SaveCalculation();
        }

        var eddTd = calculationsToSubmit.FirstOrDefault(calculation =>
            calculation.GaussianParameters?.Jobtype == JobTypes.ElectronDensityDifference);
        var eddSp = calculationsToSubmit.FirstOrDefault(calculation =>
            calculation.GaussianParameters?.Jobtype == JobTypes.SinglePoint &&
            calculation.EddSharedDirectory != null);
        if (eddBundledParameters != null && eddTd != null && eddSp != null &&
            !string.IsNullOrWhiteSpace(eddTd.MetaData?.JobId) &&
            !string.IsNullOrWhiteSpace(eddSp.MetaData?.JobId))
        {
            var postProcessing = CalculationBuilder.EddPostProcessing(
                eddBundledParameters,
                eddTd,
                eddSp,
                $"{eddTd.MetaData.JobId}:{eddSp.MetaData.JobId}",
                _sshService);
            var postJobId = _fileHandler.Upload([postProcessing]).FirstOrDefault()
                            ?? throw new InvalidOperationException(
                                "EDD postprocessing job submission failed.");
            postProcessing.MetaData!.JobId = postJobId;
            postProcessing.SaveCalculation();
        }
    }

    private static void DrawMoleculePreview(Calculation calculation)
    {
        if (!string.IsNullOrWhiteSpace(calculation.Molecule?.SmilesString))
        {
            calculation.Molecule.DrawSvg(
                calculation.Paths!.RelativeDirectory.GetPath(),
                new PythonBridge());
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
