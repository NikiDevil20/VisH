using VisH.Model.CalculationUtils;
using VisH.Model.Enums;

namespace VisH.Model.Workflow;

public sealed record WorkflowPlan(
    CalculationWorkflow Workflow,
    IReadOnlyDictionary<string, BundledConstructionParameters> Parameters);

public static class EmissionWorkflowDefinition
{
    public static WorkflowPlan Create(
        string workflowId,
        BundledConstructionParameters optimizationParameters,
        BundledConstructionParameters timeDependentParameters)
    {
        if (optimizationParameters.JobType != JobTypes.GeometryOptimization)
            throw new ArgumentException(
                "Emission optimization parameters must use GeometryOptimization.",
                nameof(optimizationParameters));
        if (timeDependentParameters.JobType != JobTypes.TimeDependant)
            throw new ArgumentException(
                "Emission parameters must use TimeDependant.",
                nameof(timeDependentParameters));

        var workflow = new CalculationWorkflow
        {
            WorkflowId = workflowId,
            WorkflowType = "Emission",
            Nodes =
            [
                new WorkflowNode
                {
                    NodeId = "excited-state-optimization",
                    Role = "S1Optimization",
                    JobType = JobTypes.GeometryOptimization,
                    Cpus = optimizationParameters.NProcs,
                    MemoryGb = optimizationParameters.Memory
                },
                new WorkflowNode
                {
                    NodeId = "emission-spectrum",
                    Role = "EmissionSpectrum",
                    JobType = JobTypes.TimeDependant,
                    Cpus = timeDependentParameters.NProcs,
                    MemoryGb = timeDependentParameters.Memory,
                    DependsOn = ["excited-state-optimization"]
                }
            ]
        };

        return new WorkflowPlan(
            workflow,
            new Dictionary<string, BundledConstructionParameters>
            {
                ["excited-state-optimization"] = optimizationParameters,
                ["emission-spectrum"] = timeDependentParameters
            });
    }
}
