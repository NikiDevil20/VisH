using System.IO;
using VisH.Model.GeneralUtils;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.CalculationProperties;

public class Paths
{
    // private
    private MetaData _metaData;
    private Molecule _molecule;
    private GaussianParameters _gaussianParameters;
    private Config _config;
    private SshService _sshService;
    
    // paths
    public DirectoryExtension CustomDirectory { get; set; }
    public DirectoryExtension RelativeDirectory { get; set; }
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
        
        var isNewCalculation = string.IsNullOrWhiteSpace(_metaData.JobName);
        if (isNewCalculation)
        {
            GetName();
        }
        
        var moleculeName = _molecule.MoleculeName;
        var state = _molecule.State.ToString();
        var jobName = _metaData.JobName!;
        
        var relativePath = Path.Combine(moleculeName, state, jobName);
        
        if (isNewCalculation)
        {
            SetDirectory(relativePath);
        }
        else
        {
            RelativeDirectory = new DirectoryExtension(relativePath, _sshService);
        }

        SetPathsWithoutJobId();
        
        if (_metaData.JobId != null)
        {
            SetPathsWithJobId();
        }
    }
    
    private void GetName()
    {
        if (!string.IsNullOrWhiteSpace(_metaData.JobName))
        {
            return;
        }

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
        RelativeDirectory = new DirectoryExtension(relativePath, _sshService);
        if (!Directory.Exists(RelativeDirectory.GetPath()))
        {
            Directory.CreateDirectory(RelativeDirectory.GetPath());
            return;
        }
        while (Directory.Exists(RelativeDirectory.GetPath()))
        {
            var uniqueName = NameGenerator.GetUniqueCalculationName(_metaData.JobName);
            var moleculeName = _molecule.MoleculeName;
            var state = _molecule.State.ToString();
            _metaData.JobName = uniqueName;
        
            relativePath = Path.Combine(moleculeName, state, uniqueName);
            RelativeDirectory = new DirectoryExtension(relativePath, _sshService);
        }
        Directory.CreateDirectory(RelativeDirectory.GetPath());
    }
    
    private void SetPathsWithoutJobId()
    {
        JsonPath = new FileExtension(RelativeDirectory.RelativePath.Append("calculation.json"), _sshService);
        GaussianInputFile = new FileExtension(RelativeDirectory.RelativePath.Append($"{_metaData.JobName}.gjf"), _sshService);
        GstartFile = new FileExtension(RelativeDirectory.RelativePath.Append("gstart"), _sshService);
        ChkFile = new FileExtension(RelativeDirectory.RelativePath.Append("gauss.chk"), _sshService);
    }

    private void SetPathsWithJobId()
    {
        LogFile = new FileExtension(RelativeDirectory.RelativePath.Append($"{_metaData.JobName}.{_metaData.JobId}.log"), _sshService);
        LgFile = new FileExtension(RelativeDirectory.RelativePath.Append($"{_metaData.JobName}.{_metaData.JobId}.lg"), _sshService);
        FChkFile = new FileExtension(RelativeDirectory.RelativePath.Append($"{_metaData.JobName}.{_metaData.JobId}.fchk"), _sshService);
    }
    
}
