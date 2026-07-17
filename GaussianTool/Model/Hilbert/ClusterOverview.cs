namespace GaussianTool.Model.Hilbert;

public class ClusterOverview
{
    
    
    public ClusterOverview()
    {
        
    }

    public static void GetOverview()
    {
        string[] jobIdsOnCluster = GetJobsOnCluster();

        foreach (var jobId in jobIdsOnCluster)
        {
            CalcStatus status = new CalcStatus(jobId);
            Console.WriteLine($"Job ID: {jobId}, State: {status.State}, Run Time: {status.RunTime}");
        }
    }

    private static string[] GetJobsOnCluster()
    {
        List<string> jobIds = new List<string>();
        
        string[] paths = JobManager.GetJobsOnCluster();
        foreach (var path in paths)
        {
            string? jobId = JobManager.GetJobId(path);
            if (jobId != null)
            {
                jobIds.Add(jobId);
            }
        }
        return jobIds.ToArray();
    }

    
}