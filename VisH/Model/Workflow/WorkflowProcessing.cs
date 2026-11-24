using System.IO;
using System.Text.Json;
using VisH.Model.CalculationObject;

namespace VisH.Model.Workflow;

public sealed record WorkflowProcessingRequest(
    string WorkflowId,
    string NodeId,
    string ProcessorType,
    IReadOnlyList<WorkflowNode> DependencyNodes);

public sealed record WorkflowProcessingResult(
    IReadOnlyList<WorkflowArtifact> Artifacts);

public interface IWorkflowProcessor
{
    WorkflowProcessingResult Process(WorkflowProcessingRequest request);
}

public sealed class DelegateWorkflowProcessor : IWorkflowProcessor
{
    private readonly IReadOnlyDictionary<string, Func<WorkflowProcessingRequest, WorkflowProcessingResult>>
        _processors;

    public DelegateWorkflowProcessor(
        IReadOnlyDictionary<string, Func<WorkflowProcessingRequest, WorkflowProcessingResult>> processors)
    {
        _processors = processors;
    }

    public WorkflowProcessingResult Process(WorkflowProcessingRequest request)
    {
        if (!_processors.TryGetValue(request.ProcessorType, out var processor))
            throw new InvalidOperationException(
                $"No workflow processor is registered for '{request.ProcessorType}'.");

        return processor(request);
    }
}

public sealed class EnergyDifferenceProcessor : IWorkflowProcessor
{
    public const string ProcessorType = "EnergyDifference";

    public WorkflowProcessingResult Process(WorkflowProcessingRequest request)
    {
        if (request.DependencyNodes.Count != 2)
            throw new InvalidOperationException(
                "Energy difference processing requires exactly two dependencies.");

        var energies = request.DependencyNodes
            .Select(GetEnergyArtifact)
            .ToArray();
        if (energies.Any(artifact => artifact.NumericValue is null))
            throw new InvalidOperationException(
                "Energy difference processing requires two numeric energy artifacts.");

        var difference = energies[1].NumericValue!.Value - energies[0].NumericValue!.Value;
        return new WorkflowProcessingResult(
        [
            new WorkflowArtifact
            {
                ArtifactId = $"{request.NodeId}-energy-difference",
                Type = "EnergyDifference",
                ProducerNodeId = request.NodeId,
                NumericValue = difference,
                Unit = energies[0].Unit ?? "Hartree"
            }
        ]);
    }

    private static WorkflowArtifact GetEnergyArtifact(WorkflowNode node)
    {
        var artifact = node.Artifacts.SingleOrDefault(candidate =>
            string.Equals(candidate.Type, "Energy", StringComparison.OrdinalIgnoreCase));
        if (artifact?.NumericValue is not null)
            return artifact;

        if (string.IsNullOrWhiteSpace(node.CalculationPath))
            throw new InvalidOperationException(
                $"Node '{node.NodeId}' has no energy artifact or calculation path.");

        var calculationJson = Path.Combine(node.CalculationPath, "calculation.json");
        if (!File.Exists(calculationJson))
            throw new InvalidOperationException(
                $"Node '{node.NodeId}' has no downloaded calculation results.");

        var calculation = JsonSerializer.Deserialize<Calculation>(
            File.ReadAllText(calculationJson));
        var energy = calculation?.Results?.ScfEnergies.LastOrDefault();
        if (energy is null)
            throw new InvalidOperationException(
                $"Node '{node.NodeId}' contains no SCF energy result.");

        return new WorkflowArtifact
        {
            ArtifactId = $"{node.NodeId}-energy",
            Type = "Energy",
            ProducerNodeId = node.NodeId,
            NumericValue = energy.Value,
            Unit = "Hartree"
        };
    }
}
