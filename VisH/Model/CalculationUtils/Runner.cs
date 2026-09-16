using Serilog;
using VisH.Model.GeneralUtils.Hilbert;


namespace VisH.Model.CalculationUtils;

public class Runner
{
    private JobStarter _jobStarter;
    private SshService _sshService;
    private FileTransferService _fileTransferService;

    public Runner(JobStarter jobStarter, SshService sshService, FileTransferService fileTransferService)
    {
        _jobStarter = jobStarter;
        _sshService = sshService;
        _fileTransferService = fileTransferService;
    }

    public void UploadJob(
        string[] localFilePaths,
        string[] remoteFilePaths,
        string clusterDirectory
        )
    {
        Console.WriteLine("Runner");
        foreach (var file in remoteFilePaths)
        {
            Console.WriteLine(file);
        }
        
        _fileTransferService.ConnectAndExecute(() =>
            _fileTransferService.UploadFiles(localFilePaths, remoteFilePaths, clusterDirectory)
            );
    }

    public string[] SubmitJobs(string[] clusterDirectories)
    {
        var jobCount = clusterDirectories.Length;
        
        var unparsedJobIds = _sshService.ConnectAndExecute(() =>
            _jobStarter.Run(clusterDirectories)
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
