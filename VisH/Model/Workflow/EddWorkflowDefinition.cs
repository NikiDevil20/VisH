using VisH.Model.CalculationUtils;
using VisH.Model.Enums;

namespace VisH.Model.Workflow;

public static class EddWorkflowDefinition
{
    public static WorkflowPlan Create(
        string workflowId,
        BundledConstructionParameters sourceParameters,
        BundledConstructionParameters densityParameters,
        BundledConstructionParameters groundStateParameters)
    {
        if (sourceParameters.JobType != JobTypes.GeometryOptimization)
            throw new ArgumentException(
                "EDD source parameters must use GeometryOptimization.",
                nameof(sourceParameters));
        if (densityParameters.State is not (State.S1 or State.S2 or State.S3))
            throw new ArgumentException(
                "EDD density parameters require state S1, S2, or S3.",
                nameof(densityParameters));
        if (groundStateParameters.JobType != JobTypes.SinglePoint)
            throw new ArgumentException(
                "EDD ground-state parameters must use SinglePoint.",
                nameof(groundStateParameters));

        var workflow = new CalculationWorkflow
        {
            WorkflowId = workflowId,
            WorkflowType = "ElectronDensityDifference",
            Nodes =
            [
                new WorkflowNode
                {
                    NodeId = "source-optimization",
                    Role = "EddSourceOptimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = sourceParameters.NProcs,
                    MemoryGb = sourceParameters.Memory
                },
                new WorkflowNode
                {
                    NodeId = "density-calculation",
                    Role = "EddDensityCalculation",
                    JobType = JobTypes.ElectronDensityDifference,
                    Cpus = densityParameters.NProcs,
                    MemoryGb = densityParameters.Memory,
                    DependsOn = ["source-optimization"]
                },
                new WorkflowNode
                {
                    NodeId = "ground-state-calculation",
                    Role = "EddGroundStateCalculation",
                    JobType = JobTypes.SinglePoint,
                    Cpus = groundStateParameters.NProcs,
                    MemoryGb = groundStateParameters.Memory,
                    DependsOn = ["source-optimization"]
                },
                new WorkflowNode
                {
                    NodeId = "edd-postprocessing",
                    Role = "EddPostProcessing",
                    JobType = JobTypes.EddPostProcessing,
                    DependsOn = ["density-calculation", "ground-state-calculation"]
                }
            ]
        };

        return new WorkflowPlan(
            workflow,
            new Dictionary<string, BundledConstructionParameters>
            {
                ["source-optimization"] = sourceParameters,
                ["density-calculation"] = densityParameters,
                ["ground-state-calculation"] = groundStateParameters
            });
    }
}
