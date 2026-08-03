using System.IO;
using GaussianTool.Model.FileHandling;

namespace GaussianTool.Model.PostRun;

public class MetaData
{
    public string? JobId { get; set; }
    public string? Functional { get; set; }
    public string? BasisSet { get; set; }
    public int? Charge { get; set; }
    public int? Multiplicity { get; set; }
    public TimeSpan? Walltime { get; set; }
    public int? Procs { get; set; }
    public int? Ram { get; set; }
    
    public MetaData(PathObject calculationDirectory)
    {
        var lgFile = calculationDirectory.GetFileWithEnding(".lg");
        var logFile = calculationDirectory.GetFileWithEnding(".log");

        var lgFileContent = File.ReadAllText(lgFile?.WindowsPath ?? string.Empty);
        var logFileContent = File.ReadAllText(logFile?.WindowsPath ?? string.Empty);

        // from lg File
        var jobId = GaussianRegex.MatchString(lgFileContent, GaussianRegex.JobIdRegex);
        var walltimeUnformatted = GaussianRegex.MatchString(lgFileContent, GaussianRegex.WalltimeRegex);
        var walltime = TimeSpan.Parse(walltimeUnformatted ?? "00:00:00");
        
        // from log file
        var functional = GaussianRegex.MatchString(logFileContent, GaussianRegex.FunctionalRegex);
        var basisSet = GaussianRegex.MatchString(logFileContent, GaussianRegex.BasisSetRegex);
        var charge = GaussianRegex.MatchInt(logFileContent, GaussianRegex.ChargeRegex);
        var multiplicity = GaussianRegex.MatchInt(logFileContent, GaussianRegex.MultiplicityRegex);
        var procs = GaussianRegex.MatchInt(logFileContent, GaussianRegex.NCpuRegex);
        var ram = GaussianRegex.MatchInt(logFileContent, GaussianRegex.RamRegex);

        JobId = jobId;
        Functional = functional;
        BasisSet = basisSet;
        Charge = charge;
        Multiplicity = multiplicity;
        Walltime = walltime;
        Procs = procs;
        Ram = ram;
    }
    
    
}