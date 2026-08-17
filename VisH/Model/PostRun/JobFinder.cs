using System.IO;
using System.Windows.Documents;
using VisH.Model.CalculationObject;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.PostRun;

public class JobFinder
{
    /// <summary>
    /// Checks if the directory contains the calculation Object which matches the given jobId.
    /// </summary>
    /// <param name="directory">PathExtension Object pointing to the directory to search</param>
    /// <param name="jobId">The job ID to match</param>
    /// <returns>The matching calculation if it matches, otherwise null</returns>
    private Calculation? CalculationMatchesJobId(DirectoryExtension directory, string jobId)
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
            
            if (calculation?.MetaData?.JobId != jobId)
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

    public Calculation? GetCalculationByJobId(string jobId)
    {
        var root = new DirectoryExtension("", new SshService());

        var calculationDirectories = GetCalculationDirs(root);

        foreach (var calculationDir in calculationDirectories)
        {
            var calculation = CalculationMatchesJobId(calculationDir, jobId);
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
    
}