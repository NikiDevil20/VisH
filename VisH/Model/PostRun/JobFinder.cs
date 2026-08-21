using System.IO;
using System.Windows.Documents;
using VisH.Model.CalculationObject;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.PostRun;

public class JobFinder
{
    private bool CalculationMatchesJobId(string jobId, Calculation calculation)
    {
        return jobId == calculation?.MetaData?.JobId;
    }

    public Calculation? GetCalculationByJobId(string jobId)
    {
        var root = new DirectoryExtension("", new SshService());

        var calculationDirectories = GetCalculationDirs(root);

        foreach (var calculationDir in calculationDirectories)
        {
            var calculation = CalculationMatchesProperty<string>(root, CalculationMatchesJobId ,jobId);
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
        var sshService = new SshService();
        var root = new DirectoryExtension("", new SshService());
        var calculations = new List<Calculation>();
        
        var pathsOnCluster = sshService.ConnectAndExecute(() => 
            JobManager.GetJobsOnCluster());

        foreach (var directory in pathsOnCluster)
        {
            Calculation? calculation = CalculationMatchesProperty<string>(root, CalculationMatchesJobId, directory.GetPath());
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
            var calculation = Calculation.FromJson(jsonPath);
            
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
    
}