using VisH.Model.FileHandling;

namespace VisH.Model.Hilbert;

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
