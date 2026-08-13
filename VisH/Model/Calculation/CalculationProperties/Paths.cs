using System.IO;
using VisH.Model.GeneralUtils;
using VisH.Model.Calculation.CalculationUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.Calculation.CalculationProperties;

public class Paths
{
    // private
    private MetaData _metaData;
    private Molecule _molecule;
    private GaussianParameters _gaussianParameters;
    private Config _config;
    private SshService _sshService;
    
    // paths
    public DirectoryExtension Directory { get; set; }
    public FileExtension GaussianInputFile { get; set; }
    public FileExtension GstartFile { get; set; }
    public FileExtension? FChkFile { get; set; }
    public FileExtension? LogFile { get; set; }
    public FileExtension? LgFile { get; set; }
    // public FileExtension? ErrorFile { get; set; }
    // public FileExtension? OutputFile { get; set; }
    public FileExtension? ChkFile { get; set; }
    public FileExtension JsonPath { get; set; }

    public Paths(
        MetaData metaData,
        Molecule molecule,
        GaussianParameters gaussianParameters,
        SshService sshService)
    {
        _metaData = metaData;
        _molecule = molecule;
        _gaussianParameters = gaussianParameters;
        _sshService = sshService;

        _config = Config.Load();
        
        var moleculeName = _molecule.MoleculeName;
        var state = _molecule.State.ToString();
        var jobName = _metaData.JobName;
        
        var relativePath = Path.Combine(moleculeName, state, jobName);
        
        GetName();
        SetDirectory(relativePath);
        SetPathsWithoutJobId(relativePath);
        
        if (_metaData.JobId != null)
        {
            SetPathsWithJobId(relativePath);
        }
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

    private void SetDirectory(string relativePath)
    {
        Directory = new DirectoryExtension(relativePath, _sshService);
    }
    
    private void SetPathsWithoutJobId(string relativePath)
    {
        JsonPath = new FileExtension(Path.Combine(relativePath, "calculation.json"), _sshService);
        GaussianInputFile = new FileExtension(Path.Combine(relativePath, $"{_metaData.JobName}.gjf"), _sshService);
        GstartFile = new FileExtension(Path.Combine(relativePath, "gstart"), _sshService);
        ChkFile = new FileExtension(Path.Combine(relativePath, "gauss.chk"), _sshService);
    }

    private void SetPathsWithJobId(string relativePath)
    {
        LogFile = new FileExtension(Path.Combine(relativePath, $"{_metaData.JobName}.{_metaData.JobId}.log"), _sshService);
        LgFile = new FileExtension(Path.Combine(relativePath, $"{_metaData.JobName}.{_metaData.JobId}.lg"), _sshService);
        FChkFile = new FileExtension(Path.Combine(relativePath, $"{_metaData.JobName}.{_metaData.JobId}.fchk"), _sshService);
    }
    
}