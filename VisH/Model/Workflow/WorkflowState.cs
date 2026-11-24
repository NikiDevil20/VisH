namespace VisH.Model.Workflow;

public enum WorkflowState
{
    Planned,
    Running,
    PartiallyCompleted,
    Successful,
    Failed
}

public enum WorkflowNodeState
{
    Planned,
    WaitingForDependencies,
    Submitted,
    Running,
    Successful,
    Failed
}

public enum WorkflowNodeKind
{
    Calculation,
    Processing
}
