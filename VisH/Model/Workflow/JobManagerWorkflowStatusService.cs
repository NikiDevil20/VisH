using VisH.Model.Enums;
using VisH.Model.CalculationObject;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.Hilbert;
using System.IO;

namespace VisH.Model.Workflow;

public sealed class JobManagerWorkflowStatusService : IWorkflowStatusService
{
    private readonly JobManager _jobManager;

    public JobManagerWorkflowStatusService(JobManager jobManager)
    {
        _jobManager = jobManager;
    }

    public JobState GetStatus(string jobId)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            throw new ArgumentException("A job ID is required.", nameof(jobId));

        var statuses = QstatParser.ParseQstat(_jobManager.QStat());
        return statuses.TryGetValue(jobId, out var status)
            ? status
            : JobState.Unknown;
    }

    public JobState GetStatus(WorkflowNode node)
    {
        var qstatStatus = GetStatus(node.JobId ??
            throw new ArgumentException("A workflow node requires a job ID.", nameof(node)));
        if (qstatStatus != JobState.Unknown || string.IsNullOrWhiteSpace(node.CalculationPath))
            return qstatStatus;

        var calculationJson = Path.Combine(node.CalculationPath, "calculation.json");
        if (!File.Exists(calculationJson))
            return JobState.Unknown;

        var calculation = Calculation.FromJson(calculationJson, _jobManager.SshService);
        calculation.RefreshStatus();
        return calculation.MetaData?.JobState ?? JobState.Unknown;
    }
}
