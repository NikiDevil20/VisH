using System.Windows.Media;
using VisH.Model.Enums;
using VisH.Model.FileHandling;
using VisH.Model.Hilbert;
using VisH.Model.PostRun;

namespace VisH.Model;

public class CalcStatus
{
    public string JobState { get; set; }
    public string JobName { get; set; }
    public string JobId { get; set; }
    public Brush LampColor { get; set; }
    public PathObject JobPath { get; set; }
    
    // public static CalcStatus Create(PathObject jobPath)
    // {
    //     
    //     var calcStatus = new CalcStatus();
    //     calcStatus.JobPath = jobPath;
    //     
    //     var jobDict = JobManager.JobStatusAndIdAsync(jobPath);
    //
    //     calcStatus.JobState = jobDict["status"];
    //
    //     calcStatus.JobName = jobDict["jobName"];
    //     calcStatus.JobId = jobDict["jobId"];
    //
    //     calcStatus.LampColor = calcStatus.JobState switch
    //     {
    //         "Successful" => new SolidColorBrush(Colors.Green),
    //         "Running" => new SolidColorBrush(Colors.Yellow),
    //         "Queue" => new SolidColorBrush(Colors.Orange),
    //         "Failed" => new SolidColorBrush(Colors.Red),
    //         _ => new SolidColorBrush(Colors.Gray)
    //     };
    //     return calcStatus;
    // }
    
    public static async Task<CalcStatus> CreateAsync(PathObject jobPath)
    {
        
        var calcStatus = new CalcStatus();
        calcStatus.JobPath = jobPath;
        
        var jobDict = await JobManager.JobStatusAndIdAsync(jobPath);

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