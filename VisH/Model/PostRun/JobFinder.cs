using System.IO;
using System.Text.Json;
using System.Windows.Documents;
using Serilog;
using VisH.Model.CalculationObject;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.PostRun;

public class JobFinder
{
    private readonly SshService _sshService;
    private readonly JobManager _jobManager;

    public JobFinder(SshService sshService, JobManager jobManager)
    {
        _sshService = sshService;
        _jobManager = jobManager;
    }

    private bool CalculationMatchesJobId(string jobId, Calculation calculation)
    {
        return jobId == calculation?.MetaData?.JobId;
    }

    public Calculation? GetCalculationByJobId(string jobId)
    {
        var root = new DirectoryExtension("", _sshService);

        var calculationDirectories = GetCalculationDirs(root);

        foreach (var calculationDir in calculationDirectories)
        {
            var calculation = CalculationMatchesProperty<string>(calculationDir, CalculationMatchesJobId, jobId);
            if (calculation != null)
            {
                return calculation;
            }
        }

        return null;
    }

    private DirectoryExtension[] GetCalculationDirs(DirectoryExtension root)
    {
        var calculationDirectories = new List<DirectoryExtension>();

        foreach (var molecule in root.GetContent().OfType<DirectoryExtension>())
        {
            foreach (var state in molecule.GetContent().OfType<DirectoryExtension>())
            {
                foreach (var calculation in state.GetContent().OfType<DirectoryExtension>())
                {
                    calculationDirectories.Add(calculation);
                }
            }
        }
        
        return calculationDirectories.ToArray();
    }

    public Calculation[] GetCalculationsOnCluster()
    {
        var calculations = new List<Calculation>();
        
        var pathsOnCluster = _sshService.ConnectAndExecute(() =>
            _jobManager.GetJobsOnCluster());
        

        foreach (var directory in pathsOnCluster)
        {
            Calculation? calculation = CalculationMatchesProperty<string>(directory, CalculationMatchesPath, directory.GetPath());
            if (calculation != null)
            {
                calculations.Add(calculation);
            }
        }
        
        return calculations.ToArray();
    }

    public Calculation[] GetSuccessfulLocalGeometryCalculations(
        string? calculationsRoot = null)
    {
        var root = calculationsRoot ?? Config.Load().LocalRechnungenPath;
        if (!Directory.Exists(root))
            throw new DirectoryNotFoundException(
                $"The local calculations directory '{root}' does not exist.");

        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new TimeSpanJsonConverter() }
        };

        var calculations = new List<Calculation>();
        foreach (var jsonPath in Directory.EnumerateFiles(
                     root,
                     "calculation.json",
                     SearchOption.AllDirectories))
        {
            var relativePath = Path.GetRelativePath(root, jsonPath);
            if (relativePath.Split(
                    [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
                    StringSplitOptions.RemoveEmptyEntries)
                .Any(part => string.Equals(part, "AlteRechnungen", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            Calculation? calculation;
            try
            {
                calculation = JsonSerializer.Deserialize<Calculation>(
                    File.ReadAllText(jsonPath),
                    options);
            }
            catch (JsonException exception)
            {
                Log.Warning(exception, "Ignoring invalid calculation data at {Path}.", jsonPath);
                continue;
            }
            catch (IOException exception)
            {
                Log.Warning(exception, "Unable to read calculation data at {Path}.", jsonPath);
                continue;
            }

            if (calculation?.GaussianParameters?.Jobtype == JobTypes.GeometryOptimization &&
                calculation.MetaData?.JobState == JobState.Successful &&
                calculation.Results?.GeometryValid == true &&
                !string.IsNullOrWhiteSpace(calculation.MetaData.JobId) &&
                calculation.Molecule is not null &&
                !string.IsNullOrWhiteSpace(calculation.Molecule.SmilesString))
            {
                calculations.Add(calculation);
            }
        }

        return calculations
            .OrderBy(calculation => calculation.Molecule!.MoleculeName)
            .ThenBy(calculation => calculation.MetaData!.JobId)
            .ToArray();
    }

    private Calculation? CalculationMatchesProperty<TResult>(
        DirectoryExtension directory,
        Func<string, Calculation, bool> propertySelector,
        string propertyValue)
    {
        const string jsonName = "calculation.json";
        string? jsonPath = null;

        if (!Directory.Exists(directory.GetPath()))
        {
            Log.Warning($"Directory {directory.GetPath()} does not exist.");
            return null;
        }
        
        var files = directory.GetContent();

        foreach (var file in files)
        {
            if (file.GetFileName() == jsonName)
            {
                jsonPath = file.GetPath();
                break;
            }
        }
        
        if (jsonPath == null)
        {
            return null;
        }
        
        try
        {
            var calculation = Calculation.FromJson(jsonPath, _sshService);
            calculation.AddPaths(string.Join(
                Path.DirectorySeparatorChar,
                directory.RelativePath));
            
            if (propertySelector(propertyValue, calculation) == false)
            {
                return null;
            }
        
            return calculation;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }

    private bool CalculationMatchesPath(string path, Calculation calculationToCheck)
    {
        return calculationToCheck.Paths.RelativeDirectory.GetPath() == path;
    }
}
