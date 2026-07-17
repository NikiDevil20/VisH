using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model.Runner;

public static class Runner
{
    public static string Run(Calculation calculation)
    {
        calculation.WriteFiles();
        
        string jobId = JobManager.StartJob(
            calculation.ClusterPath,
            [calculation.LocalGjf, calculation.LocalGstart],
            [calculation.ClusterGjf, calculation.ClusterGstart]
            );
        return jobId;
    }
}