using GaussianTool.Model.Hilbert;
using GaussianTool.Model.PostRun;

namespace GaussianTool.Model;

public class CalcStatus
{
    public string JobState { get; set; }
    public string? RunTime {get; set; }
    public string? JobName { get; set; }
    
    public CalcStatus(string jobid)
    {
        string? fullStatusText = GetStatus(jobid);
        if (!string.IsNullOrWhiteSpace(fullStatusText))
        {
            var propertiesDict = ParseQstat(fullStatusText);

            JobState = propertiesDict["job_state"] switch
            {
                "R" => "Running",
                "Q" => "In Queue",
                _ => "Unknown"
            };
            
            RunTime = propertiesDict["resources_used.walltime"];
            JobName = propertiesDict["Job_Name"];
        }
        else
        {
            JobState = "Finished";
            RunTime = null;
            JobName = jobid;
        }
    }
    
    private static string? GetStatus(string jobId)
    {
        string? status = JobManager.JobStatus(jobId);
        status = status.Replace("\n", "\r\n");
        return status;
    }

    private Dictionary<string, string> ParseQstat(string qstatOutput)
    {
        Dictionary<string, string> statusDict = new Dictionary<string, string>();

        string? currentKey = null;

        foreach (var rawLine in qstatOutput.Split('\n'))
        {
            var line = rawLine.Trim();
            
            if (line.Contains('='))
            {
                int index = line.IndexOf('=');
                currentKey = line[..index].Trim();
                statusDict[currentKey] = line[(index + 1)..].Trim();
            }
            else if (currentKey != null && !string.IsNullOrWhiteSpace(line))
            {
                statusDict[currentKey] += " " + line;
            }
        }

        return statusDict;
    }
}