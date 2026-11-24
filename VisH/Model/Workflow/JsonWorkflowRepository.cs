using System.IO;
using System.Text.Json;

namespace VisH.Model.Workflow;

public sealed class JsonWorkflowRepository : IWorkflowRepository
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    private readonly string _rootDirectory;

    public JsonWorkflowRepository(string rootDirectory)
    {
        _rootDirectory = rootDirectory;
    }

    public void Save(CalculationWorkflow workflow)
    {
        ArgumentNullException.ThrowIfNull(workflow);
        Directory.CreateDirectory(_rootDirectory);
        var path = GetPath(workflow.WorkflowId);
        File.WriteAllText(path, JsonSerializer.Serialize(workflow, Options));
    }

    public CalculationWorkflow Load(string workflowId)
    {
        if (string.IsNullOrWhiteSpace(workflowId))
            throw new ArgumentException("Workflow ID is required.", nameof(workflowId));

        var path = GetPath(workflowId);
        if (!File.Exists(path))
            throw new FileNotFoundException("Workflow manifest was not found.", path);

        return JsonSerializer.Deserialize<CalculationWorkflow>(
                   File.ReadAllText(path), Options)
               ?? throw new InvalidOperationException("Workflow manifest is invalid.");
    }

    private string GetPath(string workflowId) =>
        Path.Combine(_rootDirectory, $"{workflowId}.json");
}
