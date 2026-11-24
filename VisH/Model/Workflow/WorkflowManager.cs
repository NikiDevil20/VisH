using VisH.Model.Enums;

namespace VisH.Model.Workflow;

public sealed class WorkflowManager
{
    private readonly IWorkflowSubmissionService _submissionService;
    private readonly IWorkflowStatusService _statusService;
    private readonly IWorkflowRepository _repository;
    private readonly IWorkflowProcessor? _processor;

    public WorkflowManager(
        IWorkflowSubmissionService submissionService,
        IWorkflowStatusService statusService,
        IWorkflowRepository repository,
        IWorkflowProcessor? processor = null)
    {
        _submissionService = submissionService;
        _statusService = statusService;
        _repository = repository;
        _processor = processor;
    }

    public CalculationWorkflow Create(
        string workflowId,
        string workflowType,
        IEnumerable<WorkflowNode> nodes)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID is required.", nameof(workflowId));
        if (string.IsNullOrWhiteSpace(workflowType))
            throw new ArgumentException("Workflow type is required.", nameof(workflowType));

        var workflow = new CalculationWorkflow
        {
            WorkflowId = workflowId,
            WorkflowType = workflowType,
            Nodes = nodes.ToList()
        };
        Validate(workflow);
        _repository.Save(workflow);
        return workflow;
    }

    public CalculationWorkflow Start(CalculationWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        Validate(workflow);
        RefreshAndSchedule(workflow);
        _repository.Save(workflow);
        return workflow;
    }

    public CalculationWorkflow Refresh(string workflowId)
    {
        var workflow = _repository.Load(workflowId);
        RefreshAndSchedule(workflow);
        _repository.Save(workflow);
        return workflow;
    }

    private void RefreshAndSchedule(CalculationWorkflow workflow)
    {
        foreach (var node in workflow.Nodes.Where(node => node.JobId is not null &&
                                                          !node.IsTerminal))
        {
            var jobState = _statusService.GetStatus(node);
            node.State = jobState switch
            {
                JobState.Successful => WorkflowNodeState.Successful,
                JobState.Failed => WorkflowNodeState.Failed,
                JobState.Running => WorkflowNodeState.Running,
                JobState.Queue or JobState.Held or JobState.Begun =>
                    WorkflowNodeState.Submitted,
                _ => node.State
            };
        }

        foreach (var node in workflow.Nodes.Where(node => !node.IsTerminal))
        {
            var dependencies = node.DependsOn
                .Select(dependencyId => workflow.Nodes.Single(candidate =>
                    candidate.NodeId == dependencyId))
                .ToArray();
            if (dependencies.Any(dependency => dependency.State ==
                                               WorkflowNodeState.Failed))
            {
                node.State = WorkflowNodeState.Failed;
                node.Error = "A dependency failed.";
            }
        }

        foreach (var node in workflow.Nodes.Where(node =>
                     node.State is WorkflowNodeState.Planned or
                         WorkflowNodeState.WaitingForDependencies))
        {
            var dependencies = node.DependsOn
                .Select(dependencyId => workflow.Nodes.Single(candidate =>
                    candidate.NodeId == dependencyId))
                .ToArray();

            if (dependencies.Any(dependency =>
                    dependency.State != WorkflowNodeState.Successful))
            {
                node.State = WorkflowNodeState.WaitingForDependencies;
                continue;
            }

            try
            {
                if (node.Kind == WorkflowNodeKind.Processing)
                {
                    if (_processor is null)
                        throw new InvalidOperationException(
                            $"Workflow node '{node.NodeId}' requires a processor.");
                    if (string.IsNullOrWhiteSpace(node.ProcessorType))
                        throw new InvalidOperationException(
                            $"Workflow processing node '{node.NodeId}' has no processor type.");

                    var result = _processor.Process(new WorkflowProcessingRequest(
                        workflow.WorkflowId,
                        node.NodeId,
                        node.ProcessorType,
                        dependencies));
                    node.Artifacts = result.Artifacts.ToList();
                    node.State = WorkflowNodeState.Successful;
                    continue;
                }

                var request = new WorkflowCalculationRequest(
                    workflow.WorkflowId,
                    node.NodeId,
                    node.Role,
                    node.JobType,
                    node.Cpus,
                    node.MemoryGb,
                    dependencies.Select(dependency => dependency.JobId!).ToArray(),
                    dependencies);
                var submission = _submissionService.Submit(request);
                node.JobId = submission.JobId;
                node.CalculationPath = submission.CalculationPath;
                if (submission.Artifacts is not null)
                    node.Artifacts = submission.Artifacts.ToList();
                node.State = WorkflowNodeState.Submitted;
            }
            catch (Exception exception)
            {
                node.State = WorkflowNodeState.Failed;
                node.Error = exception.Message;
            }
        }

        workflow.State = workflow.Nodes.Any(node => node.State ==
                                                   WorkflowNodeState.Failed)
            ? WorkflowState.Failed
            : workflow.Nodes.All(node => node.State ==
                                         WorkflowNodeState.Successful)
                ? WorkflowState.Successful
                : workflow.Nodes.Any(node => node.State ==
                                             WorkflowNodeState.Successful)
                    ? WorkflowState.PartiallyCompleted
                    : WorkflowState.Running;
    }

    private static void Validate(CalculationWorkflow workflow)
    {
        if (workflow.Nodes.Count == 0)
            throw new ArgumentException("A workflow requires at least one node.");

        var ids = workflow.Nodes.Select(node => node.NodeId).ToArray();
        if (ids.Any(string.IsNullOrWhiteSpace) || ids.Distinct().Count() != ids.Length)
            throw new ArgumentException("Workflow node IDs must be unique and non-empty.");

        var knownIds = ids.ToHashSet(StringComparer.Ordinal);
        if (workflow.Nodes.Any(node => node.DependsOn.Any(dependency =>
                !knownIds.Contains(dependency))))
            throw new ArgumentException("Workflow contains an unknown dependency.");

        var visiting = new HashSet<string>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        foreach (var node in workflow.Nodes)
            Visit(node.NodeId);

        return;

        void Visit(string nodeId)
        {
            if (visited.Contains(nodeId))
                return;
            if (!visiting.Add(nodeId))
                throw new ArgumentException("Workflow dependencies must be acyclic.");

            var node = workflow.Nodes.Single(candidate => candidate.NodeId == nodeId);
            foreach (var dependency in node.DependsOn)
                Visit(dependency);

            visiting.Remove(nodeId);
            visited.Add(nodeId);
        }
    }
}
