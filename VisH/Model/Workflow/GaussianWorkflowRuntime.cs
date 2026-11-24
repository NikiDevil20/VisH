using VisH.Model.CalculationUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.Workflow;

public sealed class GaussianWorkflowRuntime
{
    private readonly FileHandler _fileHandler;
    private readonly SshService _sshService;
    private readonly JobManager _jobManager;
    private readonly string _repositoryDirectory;

    public GaussianWorkflowRuntime(
        FileHandler fileHandler,
        SshService sshService,
        JobManager jobManager,
        string repositoryDirectory)
    {
        _fileHandler = fileHandler;
        _sshService = sshService;
        _jobManager = jobManager;
        _repositoryDirectory = repositoryDirectory;
    }

    public WorkflowManager CreateManager(
        IReadOnlyDictionary<string, BundledConstructionParameters> parametersByNodeId)
    {
        return CreateManager(
            new GaussianWorkflowCalculationFactory(_sshService, parametersByNodeId));
    }

    public WorkflowManager CreateManager(WorkflowPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        IWorkflowCalculationFactory factory = plan.Workflow.WorkflowType switch
        {
            "ElectronDensityDifference" => new GaussianEddWorkflowCalculationFactory(
                _sshService,
                plan.Parameters["source-optimization"],
                plan.Parameters["density-calculation"],
                plan.Parameters["ground-state-calculation"]),
            _ => new GaussianWorkflowCalculationFactory(
                _sshService,
                plan.Parameters)
        };
        return CreateManager(factory);
    }

    private WorkflowManager CreateManager(
        IWorkflowCalculationFactory factory)
    {
        var gateway = new FileHandlerSubmissionGateway(
            _fileHandler,
            _sshService);
        var submissionService = new WorkflowCalculationSubmissionService(
            factory,
            gateway);
        var repository = new JsonWorkflowRepository(_repositoryDirectory);
        var statusService = new JobManagerWorkflowStatusService(_jobManager);

        return new WorkflowManager(
            submissionService,
            statusService,
            repository);
    }

    public CalculationWorkflow Start(
        WorkflowPlan plan)
    {
        ArgumentNullException.ThrowIfNull(plan);

        var manager = CreateManager(plan);
        var workflow = manager.Create(
            plan.Workflow.WorkflowId,
            plan.Workflow.WorkflowType,
            plan.Workflow.Nodes);
        return manager.Start(workflow);
    }
}
