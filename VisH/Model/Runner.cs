using Serilog;
using VisH.Model.FileHandling;
using VisH.Model.Hilbert;

namespace VisH.Model.Setup;

public class Runner
{
    private JobStarter _jobStarter;
    private SshService _sshService;

    public Runner(JobStarter jobStarter, SshService sshService)
    {
        _jobStarter = jobStarter;
        _sshService = sshService;
    }

    public string[] SubmitJobs(
        string[] clusterDirectories,
        string[] clusterFileNames,
        string clusterDirectory
        )
    {
        var jobCount = clusterDirectories.Length;
        
        var unparsedJobIds = _sshService.ConnectAndExecute(() =>
        {
            _jobStarter.Upload(
                clusterDirectories,
                clusterFileNames,
                clusterDirectory);

            return _jobStarter.Run(clusterDirectories);
        }
            );
        
        var jobIds = ParseJobIds(unparsedJobIds, jobCount);
        
        return jobIds;
    }

    private string[] ParseJobIds(string unparsedJobIds, int jobCount)
    {
        var splitLines = unparsedJobIds.Split('\n');
        var jobIds = new List<string>();

        foreach (var line in splitLines)
        {
            if (!line.StartsWith("__"))
            {
                jobIds.Add(line.Trim());
            }
        }
        
        if (jobIds.Count != jobCount)
        {
            Log.Warning("The number of parsed job IDs does not match the expected count.");
        }

        return jobIds.ToArray();
    }
}
