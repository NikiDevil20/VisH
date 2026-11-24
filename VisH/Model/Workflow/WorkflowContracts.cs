using System.IO;
using VisH.Model.Enums;
using VisH.Model.CalculationObject;

namespace VisH.Model.Workflow;

public sealed record WorkflowCalculationRequest(
    string WorkflowId,
    string NodeId,
    string Role,
    JobTypes JobType,
    int Cpus,
    int MemoryGb,
    IReadOnlyList<string> DependencyJobIds,
    IReadOnlyList<WorkflowNode>? DependencyNodes = null);

public sealed record WorkflowSubmissionResult(
    string JobId,
    string? CalculationPath = null,
    IReadOnlyList<WorkflowArtifact>? Artifacts = null);

public interface IWorkflowSubmissionService
{
    WorkflowSubmissionResult Submit(WorkflowCalculationRequest request);
}

public interface IWorkflowCalculationFactory
{
    Calculation Create(
        WorkflowCalculationRequest request,
        IReadOnlyList<WorkflowNode> dependencyNodes);
}

public interface ICalculationSubmissionGateway
{
    string Submit(
        Calculation calculation,
        IReadOnlyList<WorkflowNode> dependencyNodes);
}

public sealed class WorkflowCalculationSubmissionService : IWorkflowSubmissionService
{
    private readonly IWorkflowCalculationFactory _factory;
    private readonly ICalculationSubmissionGateway _submissionGateway;

    public WorkflowCalculationSubmissionService(
        IWorkflowCalculationFactory factory,
        ICalculationSubmissionGateway submissionGateway)
    {
        _factory = factory;
        _submissionGateway = submissionGateway;
    }

    public WorkflowSubmissionResult Submit(WorkflowCalculationRequest request)
    {
        var calculation = _factory.Create(request, request.DependencyNodes ?? []);
        if (calculation.MetaData is null)
            throw new InvalidOperationException("The workflow calculation has no metadata.");

        var jobId = _submissionGateway.Submit(
            calculation,
            request.DependencyNodes ?? []);
        calculation.MetaData.JobId = jobId;
        calculation.SaveCalculation();
        var calculationPath = calculation.Paths?.RelativeDirectory.GetPath();
        var artifacts = calculation.Paths?.ChkFile is null
            ? []
            : new[]
            {
                new WorkflowArtifact
                {
                    ArtifactId = $"{request.NodeId}-checkpoint",
                    Type = "Checkpoint",
                    ProducerNodeId = request.NodeId,
                    RelativePath = string.Join(
                        Path.DirectorySeparatorChar,
                        calculation.Paths.RelativeDirectory.RelativePath.Append("gauss.chk"))
                }
            };
        return new WorkflowSubmissionResult(jobId, calculationPath, artifacts);
    }
}

public interface IWorkflowStatusService
{
    JobState GetStatus(string jobId);

    JobState GetStatus(WorkflowNode node) =>
        string.IsNullOrWhiteSpace(node.JobId)
            ? JobState.Unknown
            : GetStatus(node.JobId);
}

public interface IWorkflowRepository
{
    void Save(CalculationWorkflow workflow);
    CalculationWorkflow Load(string workflowId);
}
