using VisH.Model.CalculationObject;
using VisH.Model.Enums;

namespace VisH.Model.Workflow;

public sealed class DelegateWorkflowCalculationFactory : IWorkflowCalculationFactory
{
    private readonly Dictionary<JobTypes, Func<WorkflowCalculationRequest,
        IReadOnlyList<WorkflowNode>, Calculation>> _builders = [];

    public void Register(
        JobTypes jobType,
        Func<WorkflowCalculationRequest, IReadOnlyList<WorkflowNode>, Calculation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);
        _builders[jobType] = builder;
    }

    public Calculation Create(
        WorkflowCalculationRequest request,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (!_builders.TryGetValue(request.JobType, out var builder))
        {
            throw new InvalidOperationException(
                $"No calculation builder is registered for job type '{request.JobType}'.");
        }

        return builder(request, dependencyNodes);
    }
}
