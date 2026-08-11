using System.IO;
using VisH.Model.GeneralUtils;
using VisH.Model.Calculation.CalculationUtils;

namespace VisH.Model.Calculation.CalculationProperties;

public class Paths
{
    // private
    private MetaData _metaData;
    private Molecule _molecule;
    private GaussianParameters _gaussianParameters;
    private Config _config;
    
    // paths
    
    
    public Paths(
        MetaData metaData,
        Molecule molecule,
        GaussianParameters gaussianParameters)
    {
        _metaData = metaData;
        _molecule = molecule;
        _gaussianParameters = gaussianParameters;

        _config = Config.Load();
        
        GetName();
        SetLocalDirectoryNames();
        SetClusterDirectoryNames();
        SetPaths();
    }
    
    private void GetName()
    {
        var baseName = _molecule.MoleculeName;

        var fullName = NameGenerator.GetCalculationName(
            baseName,
            _gaussianParameters.Jobtype,
            _molecule.State
        );

        _metaData.JobName = fullName;
    }

    private void SetLocalDirectoryNames()
    {
        var rechnungenPath = _config.LocalRechnungenPath;
        
        var moleculeName = _molecule.MoleculeName;
        var state = _molecule.State.ToString();
        var jobName = _metaData.JobName;
        
    }

    private void SetClusterDirectoryNames()
    {
        
    }

    private void SetPaths()
    {
        Directory.
    }
    
    private static Directory JoinCluster(this Dire)
}