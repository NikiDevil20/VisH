using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Serilog;
using VisH.Model.Enums;
using VisH.Model.PostRun;
using VisH.Model.CalculationProperties;
using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils;
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

    // Parameterless constructor for JSON deserialization
    [JsonConstructor]
    public Calculation()
    {
    }
    
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
        
        var serializedObject = JsonSerializer.Serialize(this, JsonOptions);
        
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
    public bool RefreshStatus(JobState? jobStateFromQstat = null)
    {
        if (MetaData == null) return false;
        var oldState = MetaData.JobState;
        
        if (jobStateFromQstat != null)
        {
            MetaData.JobState = jobStateFromQstat.Value;
            return oldState != MetaData.JobState;
        }

        if (string.IsNullOrWhiteSpace(MetaData.JobId))
        {
            MetaData.JobState = JobState.InPreparation;
            return oldState != MetaData.JobState;
        }

        if (Paths?.RelativeDirectory == null)
        {
            MetaData.JobState = JobState.Unknown;
            return oldState != MetaData.JobState;
        }

        try
        {
            var clusterDirectoryContent = Paths.RelativeDirectory.GetContent(PathType.Cluster);
            if (clusterDirectoryContent == null || clusterDirectoryContent.Length == 0)
            {
                MetaData.JobState = JobState.Failed;
                return oldState != MetaData.JobState;
            }

            var hasGaussChk = clusterDirectoryContent.Any(f =>
                string.Equals(f.GetFileName(PathType.Cluster), "gauss.chk", StringComparison.OrdinalIgnoreCase));
            var hasLg = clusterDirectoryContent.Any(f =>
                f.GetFileName(PathType.Cluster).EndsWith(".lg", StringComparison.OrdinalIgnoreCase));
            var hasFchk = clusterDirectoryContent.Any(f =>
                f.GetFileName(PathType.Cluster).EndsWith(".fchk", StringComparison.OrdinalIgnoreCase));
            var logFile = clusterDirectoryContent.FirstOrDefault(f =>
                f.GetFileName(PathType.Cluster).EndsWith(".log", StringComparison.OrdinalIgnoreCase));

            
            Console.WriteLine($"Calculation: {MetaData?.JobName}");

            if (!hasGaussChk || !hasLg || !hasFchk || logFile == null)
            {
                MetaData.JobState = JobState.Failed;
                return oldState != MetaData.JobState;
            }

            var logClusterPath = logFile.GetPath(PathType.Cluster);
            var escapedPath = logClusterPath.Replace("\"", "\\\"");
            
            Console.WriteLine($"Log file path: {escapedPath}");
            
            var cmd = _sshService.ConnectAndExecute(() =>
                _sshService.CommandClient.RunCommand($"cat \"{escapedPath}\""));

            var logContent = cmd?.Result ?? string.Empty;
            
            Console.WriteLine($"Log content: {logContent}");
            Console.WriteLine($"Error: {cmd?.Error}");

            var normalTermIndex = logContent.LastIndexOf("Normal termination", StringComparison.OrdinalIgnoreCase);
            var errorTermIndex = logContent.LastIndexOf("Error termination", StringComparison.OrdinalIgnoreCase);

            Console.WriteLine($"Normal termination: {normalTermIndex}, Error termination: {errorTermIndex}");
            
            if (normalTermIndex == -1 || (errorTermIndex != -1 && errorTermIndex > normalTermIndex))
            {
                MetaData.JobState = JobState.Failed;
                return oldState != MetaData.JobState;
            }

            var nimagMatches = GaussianRegex.NImagRegex.Matches(logContent);
            if (nimagMatches.Count > 0)
            {
                var lastMatch = nimagMatches[^1];
                if (int.TryParse(lastMatch.Groups[1].Value, out var nimag) && nimag != 0)
                {
                    MetaData.JobState = JobState.Imaginary;
                    return oldState != MetaData.JobState;
                }
            }

            MetaData.JobState = JobState.Successful;
            return oldState != MetaData.JobState;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed to refresh job status for {JobName}", MetaData.JobName);
            MetaData.JobState = JobState.Unknown;
            return oldState != MetaData.JobState;
        }
    }
    
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Converters = { new TimeSpanJsonConverter() },
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public void WriteFiles()
    {
        var fileWriter = new Filewriter(this);
        
        fileWriter.WriteGaussianInputFile();
        fileWriter.WriteGstartFile();
    }

    public static Calculation FromJson(string jsonPath, SshService sshService)
    {
        var jsonString = File.ReadAllText(jsonPath);
    
        Console.WriteLine(jsonString);
        
        var calculation = JsonSerializer.Deserialize<Calculation>(jsonString, JsonOptions);
        if (calculation == null)
        {
            throw new InvalidOperationException("Failed to deserialize calculation from JSON.");
        }

        Console.WriteLine($"Deserialized calculation: {calculation.MetaData?.JobName}");
        
        calculation._sshService = sshService;
        if (calculation.MetaData != null && calculation.Molecule != null && calculation.GaussianParameters != null)
            calculation.AddPaths();

        return calculation;
    }
}
