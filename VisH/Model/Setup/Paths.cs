using VisH.Model.Configs;
using VisH.Model.FileHandling;
using VisH.Model.PostRun;

namespace VisH.Model.Setup;

public class Paths
{
    // private
    private MetaData _metaData;
    private Molecule _molecule;
    private GaussianParameters _gaussianParameters;
    private Config _config;
    
    // paths
    public PathObject? LocalCalculationDirectory { get; set; }
    public PathObject? ClusterCalculationDirectory { get; set; }
    
    public PathObject? JsonPath { get; set; }
    public PathObject? GaussianInputFile { get; set; }
    public PathObject? GstartFile { get; set; }
    
    public Paths(
        MetaData metaData,
        Molecule molecule,
        GaussianParameters gaussianParameters)
    {
        _metaData = metaData;
        _molecule = molecule;
        _gaussianParameters = gaussianParameters;

        _config = Config.Load();
        
        SetLocalDirectoryNames();
        SetClusterDirectoryNames();
        SetPaths();
    }

    private void SetLocalDirectoryNames()
    {
        var rechnungenPath = _config.LocalRechnungenPath;
        
        var moleculeName = _molecule.Name;
        var state = _gaussianParameters.State.ToString();
        var jobName = _metaData.JobName;
        
        PathObject rechnungenDirectory = new PathObject(rechnungenPath, false);
        
        rechnungenDirectory = rechnungenDirectory.Join(moleculeName).Join(state).Join(jobName);
        LocalCalculationDirectory = rechnungenDirectory;
    }

    private void SetClusterDirectoryNames()
    {
        var rechnungenPath = _config.ClusterRechnungenPath;
        
        var moleculeName = _molecule.Name;
        var state = _gaussianParameters.State.ToString();
        var jobName = _metaData.JobName;
        
        PathObject rechnungenDirectory = new PathObject(rechnungenPath, true);
        
        rechnungenDirectory = rechnungenDirectory.Join(moleculeName).Join(state).Join(jobName);
        ClusterCalculationDirectory = rechnungenDirectory;
    }

    private void SetPaths()
    {
        var jobName = _metaData.JobName;
        JsonPath = LocalCalculationDirectory?.Join("calculation.json");
        GaussianInputFile = LocalCalculationDirectory?.Join($"{jobName}.gjf");
        GstartFile = LocalCalculationDirectory?.Join("gstart");
    }
}