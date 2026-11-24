namespace VisH.Model.Workflow;

public sealed class WorkflowArtifact
{
    public string ArtifactId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string ProducerNodeId { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string? Unit { get; set; }
    public double? NumericValue { get; set; }
}
