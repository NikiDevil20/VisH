using VisH.Model.Hilbert;

namespace VisH.Model.Runner;

public static class Runner
{
    public static string Run(Calculation calculation)
    {
        calculation.WriteFiles();
        
        string jobId = JobManager.StartJob(
            calculation.GjfPath.ClusterPath,
            [calculation.GjfPath.WindowsPath, calculation.GstartPath.WindowsPath],
            [calculation.GjfPath.ClusterPath, calculation.GstartPath.ClusterPath]
            );
        return jobId;
    }
}
