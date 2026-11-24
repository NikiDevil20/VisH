using VisH.Model.Enums;

namespace VisH.Model.Workflow;

public sealed class WorkflowNode
{
    public string NodeId { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public WorkflowNodeKind Kind { get; set; } = WorkflowNodeKind.Calculation;
    public string? ProcessorType { get; set; }
    public JobTypes JobType { get; set; }
    public int Cpus { get; set; }
    public int MemoryGb { get; set; }
    public List<string> DependsOn { get; set; } = [];
    public string? JobId { get; set; }
    public string? CalculationPath { get; set; }
    public List<WorkflowArtifact> Artifacts { get; set; } = [];
    public WorkflowNodeState State { get; set; } = WorkflowNodeState.Planned;
    public string? Error { get; set; }

    public bool IsTerminal =>
        State is WorkflowNodeState.Successful or WorkflowNodeState.Failed;
}
