using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model.Runner;

public static class Runner
{
    public static void Run(Calculation calculation)
    {
        calculation.WriteFiles();
        
        JobManager.StartJob(
            calculation.ClusterPath,
            [calculation.LocalGjf, calculation.LocalGstart],
            [calculation.ClusterGjf, calculation.ClusterGstart]
            );
    }
}