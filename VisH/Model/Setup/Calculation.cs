using System.IO;
using System.Text.Json;
using Serilog;
using VisH.Model.Enums;
using VisH.Model.PostRun;

namespace VisH.Model.Setup;

public class Calculation
{
    public MetaData? MetaData { get; set; }
    public Results? Results { get; set; }
    public GaussianParameters? GaussianParameters { get; set; }
    public Molecule? Molecule { get; set; }
    
    private Runner? Runner { get; set; }
    public Paths? Paths { get; set; }


    public Calculation()
    {
        
    }
    
    public void AddMetaData(MetaData? metaData)
    {
        if (metaData == null)
        {
            var ex = new InvalidOperationException(nameof(metaData));
            Log.Error(ex, "Cannot add null metadata.");
            throw ex;
        }
        
        Log.Information(
            "Metadata added to calculation.");
        MetaData = metaData;
    }
    
    public void AddResults(Results? results)
    {
        if (results == null)
        {
            var ex = new InvalidOperationException(nameof(results));
            Log.Error(ex, "Cannot add null results.");
            throw ex;
        }

        Log.Information(
            "Results added to calculation.");
        Results = results;
    }
    
    public void AddGaussianParameters(GaussianParameters? gaussianParameters)
    {
        if (gaussianParameters == null)
        {
            var ex = new InvalidOperationException(nameof(gaussianParameters));
            Log.Error(ex, "Cannot add null Gaussian parameters.");
            throw ex;
        }
        
        if (Molecule == null)
        {
            var ex = new InvalidOperationException(nameof(Molecule));
            Log.Error(ex, "Cannot add metadata without a molecule.");
            throw ex;
        }
        
        Log.Information(
            "Gaussian parameters added to calculation.");
        GaussianParameters = gaussianParameters;
    }

    public void AddPaths()
    {
        if (MetaData == null)
        {
            var ex = new InvalidOperationException(nameof(MetaData));
            Log.Error(ex, "Cannot add paths without metadata.");
            throw ex;
        }
        
        if (Molecule == null)
        {
            var ex = new InvalidOperationException(nameof(Molecule));
            Log.Error(ex, "Cannot add paths without a molecule.");
            throw ex;
        }

        if (GaussianParameters == null)
        {
            var ex = new InvalidOperationException(nameof(GaussianParameters));
            Log.Error(ex, "Cannot add paths without Gaussian parameters.");
            throw ex;
        }

        var paths = new Paths(MetaData, Molecule, GaussianParameters);
        
        Log.Information(
            "Paths for job {JobName} added to calculation.",
            MetaData.JobName);
        Paths = paths;
    }
    
    public void AddMolecule(Molecule? molecule)
    {
        if (molecule == null)
        {
            var ex = new InvalidOperationException(nameof(molecule));
            Log.Error(ex, "Cannot add null molecule.");
            throw ex;
        }
        
        Log.Information(
            "Molecule added to calculation.");
        Molecule = molecule;
    }
    
    public void AddRunner(Runner? runner)
    {
        if (runner == null)
        {
            var ex = new InvalidOperationException(nameof(runner));
            Log.Error(ex, "Cannot add null runner.");
            throw ex;
        }

        Log.Information(
            "Runner added to calculation.");
        Runner = runner;
    }
    
    
    public bool CheckCompletion()
    {
        if (MetaData == null) return false;
        if (Molecule == null) return false;
        if (GaussianParameters == null) return false;
        if (Paths == null) return false;
        return true;
    }

    public bool AllFilesPresent()
    {
        if (!CheckCompletion()) return false;
        
        if (!File.Exists(Paths?.JsonPath?.WindowsPath))
        {
            return false;
        }

        if (!File.Exists(Paths.GaussianInputFile?.WindowsPath))
        {
            return false;
        }

        if (!File.Exists(Paths.GstartFile?.WindowsPath))
        {
            return false;
        }

        return true;
    }

    public void SaveCalculation()
    {
        if (!CheckCompletion())
        {
            var ex = new InvalidOperationException("Calculation is not complete.");
            Log.Error(ex, "Cannot save incomplete calculation.");
            throw ex;
        }

        if (Paths?.LocalCalculationDirectory == null)
        {
            var ex = new InvalidOperationException(nameof(Paths.LocalCalculationDirectory));
            Log.Error(ex, "Cannot save calculation without a local calculation directory.");
            throw ex;
        }

        Directory.CreateDirectory(Paths.LocalCalculationDirectory.WindowsPath);
        
        var jsonPath = Paths.LocalCalculationDirectory.Join("calculation.json");
        
        var serializedObject = JsonSerializer.Serialize(this);

        try
        {
            File.WriteAllText(jsonPath.WindowsPath, serializedObject);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save calculation to {Path}.", jsonPath.WindowsPath);
            throw;
        }
        Log.Information("Calculation {JobName} saved to {Path}.",
            MetaData?.JobName, jsonPath.WindowsPath);
    }

    /// <summary>
    /// Refreshes the status and returns whether the status has changed.
    /// </summary>
    /// <returns>status has changed boolean</returns>
    public bool RefreshStatus()
    {
        var oldState = MetaData.JobState;
        
        if (oldState is JobState.Failed or JobState.Successful)
        {
            // no need to refresh terminal states.
            return false;
        }
        
        
        

        return true;
    }
    
}