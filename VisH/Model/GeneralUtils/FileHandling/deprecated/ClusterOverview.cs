using VisH.Model.GeneralUtils.FileHandling;

namespace VisH.Model.GeneralUtils.Hilbert;

public class ClusterOverview
{
    
    
    public ClusterOverview()
    {
        
    }

    public static void GetOverview()
    {
        PathObject[] jobsOnCluster = GetJobsOnCluster();

        foreach (var job in jobsOnCluster)
        {
            CalcStatus status = new CalcStatus();
            Console.WriteLine($"Job ID: {job.ClusterPath}, State: {status.JobState}");
        }
    }

    public static PathObject[] GetJobsOnCluster()
    {
        List<PathObject> jobs = new List<PathObject>();
        
        PathObject[] paths = JobManager.GetJobsOnCluster();
        foreach (var path in paths)
        {
            string? jobId = JobManager.GetJobId(path);
            if (jobId != null)
            {
                jobs.Add(path);
            }
        }
        return jobs.ToArray();
    }
    
    
}
