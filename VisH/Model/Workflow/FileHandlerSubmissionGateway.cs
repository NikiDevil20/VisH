using VisH.Model.CalculationObject;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.Workflow;

public sealed class FileHandlerSubmissionGateway : ICalculationSubmissionGateway
{
    private readonly FileHandler _fileHandler;
    private readonly SshService _sshService;

    public FileHandlerSubmissionGateway(
        FileHandler fileHandler,
        SshService sshService)
    {
        _fileHandler = fileHandler;
        _sshService = sshService;
    }

    public string Submit(
        Calculation calculation,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        var jobIds = _fileHandler.Upload(
            [calculation],
            _ => CopyDependencyCheckpoints(calculation, dependencyNodes));
        if (jobIds.Length != 1 || string.IsNullOrWhiteSpace(jobIds[0]))
            throw new InvalidOperationException("The calculation submission did not return exactly one job ID.");

        return jobIds[0];
    }

    private void CopyDependencyCheckpoints(
        Calculation calculation,
        IReadOnlyList<WorkflowNode> dependencyNodes)
    {
        if (dependencyNodes.Count == 0)
            return;

        if (calculation.Paths?.ChkFile is null)
            throw new InvalidOperationException(
                "The dependent calculation has no checkpoint destination.");

        var checkpointArtifacts = dependencyNodes
            .SelectMany(node => node.Artifacts)
            .Where(artifact => string.Equals(
                artifact.Type,
                "Checkpoint",
                StringComparison.OrdinalIgnoreCase))
            .ToArray();

        if (checkpointArtifacts.Length != 1)
            throw new InvalidOperationException(
                "A dependent calculation requires exactly one checkpoint artifact.");

        var source = new FileExtension(
            checkpointArtifacts[0].RelativePath,
            _sshService);
        _fileHandler.CopyCheckpoint(source, calculation.Paths.ChkFile);
    }
}
