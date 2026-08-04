using VisH.Model.Hilbert;

namespace VisH.Model.FileHandling;

public class UploadManager
{
    public bool ClusterWasChanged { get; set; }
    
    

    public string Run(Calculation calculation)
    {
        calculation.WriteFiles();
        
        string jobId = JobManager.StartJob(
            calculation.GjfPath.ClusterFolder,
            [calculation.GjfPath.WindowsPath, calculation.GstartPath.WindowsPath],
            [calculation.GjfPath.ClusterPath, calculation.GstartPath.ClusterPath]
        );
        ClusterWasChanged = true;
        return jobId;
    }
}
