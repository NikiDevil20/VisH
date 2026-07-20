using System.Drawing;
using GaussianTool.Model.Hilbert;
using GaussianTool.Model.PostRun;

namespace GaussianTool.Model;

public class CalcStatus
{
    public string JobState { get; set; }
    public string JobName { get; set; }
    public string JobId { get; set; }
    public Brush LampColor { get; set; }
    
    public static async Task<CalcStatus> CreateAsync(string jobPath)
    {
        var calcStatus = new CalcStatus();
        
        var jobDict = JobManager.JobStatusAndId(jobPath);
    
        calcStatus.JobState = jobDict["status"] switch
        {
            "R" => "Running",
            "Q" => "In Queue",
            "F" => "Failed",
            "S" => "Successful",
            _ => "Unknown"
        };
    
        calcStatus.JobName = jobDict["jobName"];
        calcStatus.JobId = jobDict["jobId"];
    
        calcStatus.LampColor = calcStatus.JobState switch
        {
            "Successful" => new SolidBrush(Color.Green),
            "Running" => new SolidBrush(Color.Yellow),
            "In Queue" => new SolidBrush(Color.Orange),
            "Failed" => new SolidBrush(Color.Red),
            _ => new SolidBrush(Color.Gray)
        };
        return calcStatus;
    }
    
    
    // private static string? GetStatus(string jobId)
    // {
    //     string? status = JobManager.JobStatus(jobId);
    //     status = status.Replace("\n", "\r\n");
    //     return status;
    // }

    // private Dictionary<string, string> ParseQstat(string qstatOutput)
    // {
    //     Dictionary<string, string> statusDict = new Dictionary<string, string>();
    //
    //     string? currentKey = null;
    //
    //     foreach (var rawLine in qstatOutput.Split('\n'))
    //     {
    //         var line = rawLine.Trim();
    //         
    //         if (line.Contains('='))
    //         {
    //             int index = line.IndexOf('=');
    //             currentKey = line[..index].Trim();
    //             statusDict[currentKey] = line[(index + 1)..].Trim();
    //         }
    //         else if (currentKey != null && !string.IsNullOrWhiteSpace(line))
    //         {
    //             statusDict[currentKey] += " " + line;
    //         }
    //     }
    //
    //     return statusDict;
    // }
}