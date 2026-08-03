using System.Windows.Media;
using GaussianTool.Model.Enums;
using GaussianTool.Model.FileHandling;
using GaussianTool.Model.Hilbert;
using GaussianTool.Model.PostRun;

namespace GaussianTool.Model;

public class CalcStatus
{
    public string JobState { get; set; }
    public string JobName { get; set; }
    public string JobId { get; set; }
    public Brush LampColor { get; set; }
    public PathObject JobPath { get; set; }
    
    public static CalcStatus Create(PathObject jobPath)
    {
        
        var calcStatus = new CalcStatus();
        calcStatus.JobPath = jobPath;
        
        var jobDict = JobManager.JobStatusAndId(jobPath);

        calcStatus.JobState = jobDict["status"];
    
        calcStatus.JobName = jobDict["jobName"];
        calcStatus.JobId = jobDict["jobId"];
    
        calcStatus.LampColor = calcStatus.JobState switch
        {
            "Successful" => new SolidColorBrush(Colors.Green),
            "Running" => new SolidColorBrush(Colors.Yellow),
            "Queue" => new SolidColorBrush(Colors.Orange),
            "Failed" => new SolidColorBrush(Colors.Red),
            _ => new SolidColorBrush(Colors.Gray)
        };
        return calcStatus;
    }
}