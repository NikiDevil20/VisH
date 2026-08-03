using System.IO;
using GaussianTool.Model.FileHandling;

namespace GaussianTool.Model.PostRun;

public class MetaData
{
    public string JobName { get; set; }
    public string JobId { get; set; }
    public string Functional { get; set; }
    public string BasisSet { get; set; }
    public string Charge { get; set; }
    public string Multiplicity { get; set; }
    public string Walltime { get; set; }
    public string Procs { get; set; }
    public string Ram { get; set; }
    
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
        var jobName = GaussianRegex.MatchString(lgFileContent, GaussianRegex.JobNameRegex);
        
        // from log file
        var functional = GaussianRegex.MatchString(logFileContent, GaussianRegex.FunctionalRegex);
        var basisSet = GaussianRegex.MatchString(logFileContent, GaussianRegex.BasisSetRegex);
        var charge = GaussianRegex.MatchInt(logFileContent, GaussianRegex.ChargeRegex);
        var multiplicity = GaussianRegex.MatchInt(logFileContent, GaussianRegex.MultiplicityRegex);
        var procs = GaussianRegex.MatchInt(logFileContent, GaussianRegex.NCpuRegex);
        var ram = GaussianRegex.MatchInt(logFileContent, GaussianRegex.RamRegex);

        JobName = jobName ?? "not detected";
        JobId = jobId ?? "not detected";
        Functional = functional ?? "not detected";
        BasisSet = basisSet ?? "not detected";
        Charge = charge?.ToString() ?? "not detected";
        Multiplicity = multiplicity?.ToString() ?? "not detected";
        Walltime = walltime.ToString() ?? "not detected";
        Procs = procs?.ToString() ?? "not detected";
        Ram = ram?.ToString() ?? "not detected";
    }
    
    public Dictionary<string, string> ToDictionary()
    {
        return new Dictionary<string, string>()
        {
            { "JobName", JobName },
            { "JobId", JobId },
            { "Functional", Functional },
            { "BasisSet", BasisSet },
            { "Charge", Charge },
            { "Multiplicity", Multiplicity },
            { "Walltime", Walltime },
            { "Procs", Procs },
            { "Ram", Ram }
        };
    }

}