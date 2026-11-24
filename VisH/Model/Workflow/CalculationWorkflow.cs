using System.Text.Json.Serialization;

namespace VisH.Model.Workflow;

public sealed class CalculationWorkflow
{
    public string WorkflowId { get; set; } = string.Empty;
    public string WorkflowType { get; set; } = string.Empty;
    public WorkflowState State { get; set; } = WorkflowState.Planned;
    public List<WorkflowNode> Nodes { get; set; } = [];

    [JsonIgnore]
    public int CompletedNodeCount => Nodes.Count(node =>
        node.State == WorkflowNodeState.Successful);

    [JsonIgnore]
    public int TotalNodeCount => Nodes.Count;

    [JsonIgnore]
    public double CompletionPercentage =>
        TotalNodeCount == 0 ? 0 : CompletedNodeCount * 100d / TotalNodeCount;
}
