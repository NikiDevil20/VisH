using System.IO;

namespace GaussianTool.Model.PostRun;

public class MetaData
{
    public string? JobId { get; set; }
    public string Functional { get; set; }
    public string BasisSet { get; set; }
    public int Charge { get; set; }
    public int Multiplicity { get; set; }
    public double Walltime { get; set; }
    public int Procs { get; set; }
    public int Ram { get; set; }
    public bool Converged { get; set; }
    public int Steps {get; set;}
    
    public MetaData(string directoryPath)
    {
        string[] files;
        try
        {
            files = Directory.GetFiles(directoryPath);
        }
        catch (DirectoryNotFoundException e)
        {
            Console.WriteLine(e);
            throw;
        }

        string logText = "";
        string fileName = "";
        foreach (var file in files)
        {
            if (file.EndsWith(".log"))
            {
                logText = File.ReadAllText(file);
                fileName = Path.GetFileName(file);
                break;
            }
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new FileNotFoundException("No .log file found in the specified directory.");
        }
        JobId = GaussianRegex.MatchString(fileName, @"\.(\d+\.hpc-batch)\.log$");
        
    }
}