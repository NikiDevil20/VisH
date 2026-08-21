using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using VisH.Model.Enums;
using VisH.Model.PostRun;
using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.CalculationObject;

public class Calculation
{
    
    public MetaData? MetaData { get; set; }
    public Results? Results { get; set; }
    public GaussianParameters? GaussianParameters { get; set; }
    public Molecule? Molecule { get; set; }
    
    private SshService _sshService;
    [JsonIgnore]
    public Paths? Paths { get; set; }

    public Calculation(SshService sshService)
    {
        _sshService = sshService;
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

        var paths = new Paths(MetaData, Molecule, GaussianParameters, _sshService);
        
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
        
        if (!File.Exists(Paths?.JsonPath.GetPath()))
        {
            return false;
        }

        if (!File.Exists(Paths.GaussianInputFile.GetPath()))
        {
            return false;
        }

        if (!File.Exists(Paths.GstartFile.GetPath()))
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

        if (Paths?.RelativeDirectory.GetPath() == null)
        {
            var ex = new InvalidOperationException(nameof(Paths.RelativeDirectory));
            Log.Error(ex, "Cannot save calculation without a local calculation directory.");
            throw ex;
        }

        Directory.CreateDirectory(Paths.RelativeDirectory.GetPath());
        
        var serializedObject = JsonSerializer.Serialize(this);

        try
        {
            File.WriteAllText(Paths.JsonPath.GetPath(), serializedObject);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save calculation to {Path}.", Paths.JsonPath.GetPath());
            throw;
        }
        Log.Information("Calculation {JobName} saved to {Path}.",
            MetaData?.JobName, Paths.JsonPath.GetPath());
    }

    /// <summary>
    /// Refreshes the status and returns whether the status has changed.
    /// </summary>
    /// <returns>status has changed boolean</returns>
    public bool RefreshStatus(JobState? jobStateFromQstat=null)
    {
        var oldState = MetaData.JobState;
        
        // TODO
        
        if (oldState is JobState.Failed or JobState.Successful)
        {
            // no need to refresh terminal states.
            return false;
        }

        var clusterDirectoryContent = Paths.RelativeDirectory.GetContent(PathType.Cluster);
        

        return true;
    }

    public void WriteFiles()
    {
        var fileWriter = new Filewriter(this);
        
        fileWriter.WriteGaussianInputFile();
        fileWriter.WriteGstartFile();
    }

    public static Calculation FromJson(string jsonPath)
    {
        var jsonString = File.ReadAllText(jsonPath);
        
        var calculation = JsonSerializer.Deserialize<Calculation>(jsonString);
        
        return calculation ?? throw new InvalidOperationException("Failed to deserialize calculation from JSON.");
    }
}