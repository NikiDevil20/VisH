using System.IO;
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
        Console.WriteLine($"Checking if {calculationToCheck.Paths.RelativeDirectory.GetPath()} == {path}");
        return calculationToCheck.Paths.RelativeDirectory.GetPath() == path;
    }
}
