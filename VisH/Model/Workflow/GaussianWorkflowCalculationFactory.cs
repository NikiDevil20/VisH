using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.Workflow;

public sealed class GaussianWorkflowCalculationFactory : IWorkflowCalculationFactory
{
    private readonly SshService _sshService;
    private readonly IReadOnlyDictionary<string, BundledConstructionParameters>
        _parametersByNodeId;

    public GaussianWorkflowCalculationFactory(
        SshService sshService,
        IReadOnlyDictionary<string, BundledConstructionParameters> parametersByNodeId)
    {
        _sshService = sshService;
        _parametersByNodeId = parametersByNodeId;
    }

    public Calculation Create(
        WorkflowCalculationRequest request,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (!_parametersByNodeId.TryGetValue(request.NodeId, out var parameters))
        {
            throw new InvalidOperationException(
                $"No Gaussian parameters are configured for workflow node '{request.NodeId}'.");
        }

        if (parameters.JobType != request.JobType)
        {
            throw new InvalidOperationException(
                $"Workflow node '{request.NodeId}' requests {request.JobType}, " +
                $"but its Gaussian parameters specify {parameters.JobType}.");
        }

        return request.JobType switch
        {
            JobTypes.GeometryOptimization =>
                CalculationBuilder.GeometryOptimization(parameters, _sshService),
            JobTypes.TimeDependant => CreateTimeDependent(
                parameters,
                dependencyNodes),
            JobTypes.SinglePoint => CreateSinglePoint(
                parameters,
                dependencyNodes),
            _ => throw new NotSupportedException(
                $"Gaussian workflow creation does not support {request.JobType}.")
        };
    }

    private Calculation CreateTimeDependent(
        BundledConstructionParameters parameters,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        var dependency = GetSingleDependency(dependencyNodes);
        var checkpointPath = GetCheckpointPath(dependency);
        var jobId = GetJobId(dependency);

        return CalculationBuilder.TimeDependant(
            parameters,
            jobId,
            checkpointPath,
            _sshService);
    }

    private Calculation CreateSinglePoint(
        BundledConstructionParameters parameters,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        var dependency = GetSingleDependency(dependencyNodes);
        return CalculationBuilder.SinglePoint(
            parameters,
            GetJobId(dependency),
            _sshService);
    }

    private static WorkflowNode GetSingleDependency(
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (dependencyNodes.Count != 1)
        {
            throw new InvalidOperationException(
                "This Gaussian calculation requires exactly one dependency.");
        }

        return dependencyNodes[0];
    }

    private static string GetJobId(WorkflowNode dependency)
    {
        if (string.IsNullOrWhiteSpace(dependency.JobId))
            throw new InvalidOperationException(
                $"Dependency '{dependency.NodeId}' has no submitted job ID.");

        return dependency.JobId;
    }

    private static string GetCheckpointPath(WorkflowNode dependency)
    {
        var checkpoint = dependency.Artifacts.SingleOrDefault(artifact =>
            string.Equals(artifact.Type, "Checkpoint", StringComparison.OrdinalIgnoreCase));
        if (checkpoint is null || string.IsNullOrWhiteSpace(checkpoint.RelativePath))
        {
            throw new InvalidOperationException(
                $"Dependency '{dependency.NodeId}' does not provide a checkpoint artifact.");
        }

        return checkpoint.RelativePath;
    }
}
