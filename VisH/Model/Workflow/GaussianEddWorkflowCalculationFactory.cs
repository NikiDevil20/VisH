using System.IO;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.Workflow;

public sealed class GaussianEddWorkflowCalculationFactory : IWorkflowCalculationFactory
{
    private readonly SshService _sshService;
    private readonly BundledConstructionParameters _sourceParameters;
    private readonly BundledConstructionParameters _densityParameters;
    private readonly BundledConstructionParameters _groundStateParameters;
    private (Calculation TimeDependent, Calculation GroundState)? _eddPair;

    public GaussianEddWorkflowCalculationFactory(
        SshService sshService,
        BundledConstructionParameters sourceParameters,
        BundledConstructionParameters densityParameters,
        BundledConstructionParameters groundStateParameters)
    {
        _sshService = sshService;
        _sourceParameters = sourceParameters;
        _densityParameters = densityParameters;
        _groundStateParameters = groundStateParameters;
    }

    public Calculation Create(
        WorkflowCalculationRequest request,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        return request.NodeId switch
        {
            "source-optimization" => CalculationBuilder.GeometryOptimization(
                _sourceParameters,
                _sshService),
            "density-calculation" => GetEddPair(dependencyNodes).TimeDependent,
            "ground-state-calculation" => GetEddPair(dependencyNodes).GroundState,
            "edd-postprocessing" => CreatePostProcessing(dependencyNodes),
            _ => throw new NotSupportedException(
                $"EDD Gaussian workflow does not support node '{request.NodeId}'.")
        };
    }

    private (Calculation TimeDependent, Calculation GroundState) GetEddPair(
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (_eddPair is not null)
            return _eddPair.Value;
        if (dependencyNodes.Count != 1)
            throw new InvalidOperationException(
                "EDD calculations require exactly one source optimization dependency.");

        var sourceNode = dependencyNodes[0];
        var source = LoadCalculation(sourceNode);
        if (source.GaussianParameters?.Jobtype != JobTypes.GeometryOptimization ||
            source.Paths?.ChkFile is null ||
            source.MetaData?.JobId is null)
        {
            throw new InvalidOperationException(
                "The EDD source must be a geometry optimization with a checkpoint and job ID.");
        }

        _eddPair = CalculationBuilder.ElectronDensityDifference(
            _densityParameters,
            source,
            _sshService,
            sourceIsAlreadyComplete: false,
            groundStateParameters: _groundStateParameters);
        return _eddPair.Value;
    }

    private Calculation CreatePostProcessing(
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (dependencyNodes.Count != 2)
            throw new InvalidOperationException(
                "EDD postprocessing requires density and ground-state dependencies.");

        var timeDependent = LoadCalculation(dependencyNodes.Single(node =>
            node.NodeId == "density-calculation"));
        var groundState = LoadCalculation(dependencyNodes.Single(node =>
            node.NodeId == "ground-state-calculation"));
        var dependencyJobIds = string.Join(
            ":",
            timeDependent.MetaData?.JobId,
            groundState.MetaData?.JobId);

        if (string.IsNullOrWhiteSpace(timeDependent.MetaData?.JobId) ||
            string.IsNullOrWhiteSpace(groundState.MetaData?.JobId))
        {
            throw new InvalidOperationException(
                "EDD postprocessing dependencies must have submitted job IDs.");
        }

        return CalculationBuilder.EddPostProcessing(
            _densityParameters,
            timeDependent,
            groundState,
            dependencyJobIds,
            _sshService);
    }

    private Calculation LoadCalculation(WorkflowNode node)
    {
        if (string.IsNullOrWhiteSpace(node.CalculationPath))
            throw new InvalidOperationException(
                $"Workflow node '{node.NodeId}' has no calculation path.");

        var calculationJson = Path.Combine(node.CalculationPath, "calculation.json");
        if (!File.Exists(calculationJson))
            throw new FileNotFoundException(
                $"Calculation data for workflow node '{node.NodeId}' was not found.",
                calculationJson);

        return Calculation.FromJson(calculationJson, _sshService);
    }
}
