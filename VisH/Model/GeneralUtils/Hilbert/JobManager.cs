using System.IO;
using Serilog;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.PostRun;

namespace VisH.Model.GeneralUtils.Hilbert;

public class JobManager
{
    private readonly SshService _sshService;
    private readonly FileTransferService _fileTransferService;

    public JobManager(SshService sshService, FileTransferService fileTransferService)
    {
        _sshService = sshService;
        _fileTransferService = fileTransferService;
    }
    
    public void Connect()
    {
        _sshService.Connect();
    }
    
    public void Disconnect()
    {
        _sshService.Disconnect();
    }
    
    
    private bool NormalTermination(FileExtension logFile)
    {
        var cmd = _sshService.CommandClient.RunCommand(
            $"tail {logFile.GetPath(PathType.Cluster)}");
        return cmd.Result.Contains("Normal termination");
    }
    
    // public static Dictionary<string, string> JobStatusAndId(PathObject path)
    // {
    //     bool logExists = false;
    //     bool qstatOutput = false;
    //     bool normalTermination = false;
    //
    //     string? jobId = null;
    //     State status = State.Queue;
    //
    //     Dictionary<string, string> jobInfo = new Dictionary<string, string>();
    //     
    //     if (path.DestinationType != PathType.Directory)
    //         throw new ArgumentException("Path must be a directory.");
    //
    //     if (path.FolderContent == null || path.FolderContent.Length == 0)
    //         throw new FileNotFoundException("Directory is empty.");
    //     
    //     PathObject[] files = path.GetFolderContent();
    //     
    //     foreach (var file in files)
    //     {
    //        if (file.ClusterPath.EndsWith(".log")) 
    //        {
    //            logExists = true;
    //            jobId = GaussianRegex.MatchString(file.ClusterPath, GaussianRegex.JobIdLogFile);
    //
    //            normalTermination = NormalTermination(file);
    //            if (!normalTermination)
    //            {
    //                if (jobId != null)
    //                {
    //                    var qstat = _sshService.CommandClient.RunCommand($"qstat {jobId}");
    //                    if (!string.IsNullOrWhiteSpace(qstat.Result))
    //                    {
    //                        qstatOutput = true;
    //                    }
    //                }
    //            }
    //        }
    //     }
    //     if (logExists && qstatOutput)
    //     {
    //         status = State.Running;
    //     }
    //     else if (logExists && !qstatOutput)
    //     {
    //         status = State.Failed;
    //     }
    //     if (normalTermination)
    //     {
    //         status = State.Successful;
    //     }
    //     
    //     jobInfo["jobName"] = Path.GetFileName(path.ClusterPath);
    //     jobInfo["jobId"] = jobId;
    //     jobInfo["status"] = status.ToString();
    //     return jobInfo;
    //     
    // }

    public string DeleteJob(string jobId)
    {
        var cmd = _sshService.CommandClient.RunCommand($"qdel {jobId}");
        return cmd.Result;
    }

    public PathObject[] Dir(PathObject path, PathType? onlyListOneType = null)
    {
        List<PathObject> paths = new List<PathObject>();
        
        var cmd = _sshService.CommandClient.RunCommand(
            $"cd \"{path.ClusterPath}\" && find . -maxdepth 1 -mindepth 1 -printf \"%f\\n\"");
        string fullString = cmd.Result;
        
        string[] splitString = fullString.Split(
            ["\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var relativePath in splitString)
        {
            var joinedPath = path.Join(relativePath);
            if (onlyListOneType.HasValue)
            {
                if (joinedPath.DestinationType == onlyListOneType.Value)
                {
                    paths.Add(joinedPath);
                }
            }
            else
            {
                paths.Add(joinedPath);
            }
        }
        
        return paths.ToArray();
    }
    
    public long FileSize(PathObject path)
    {
        try
        {
            _fileTransferService.Connect();
            if (path.DestinationType == PathType.Directory)
            {
                return 0;
            }

            var attributes = _fileTransferService.FileClient.GetAttributes(path.ClusterPath);
            return attributes.Size;
        }
        finally
        {
            _fileTransferService.Disconnect();
        }
    }
    
    private void DownloadFile(string path, Stream fileStream, Action<ulong> downloadCallback)
    {
        _fileTransferService.FileClient.DownloadFile(path, fileStream, downloadCallback);
    }
    
    public void DownloadFolder(
        DirectoryExtension directory,
        IProgress<DownloadProgress> progress)
    {
        _fileTransferService.Connect();
        try
        {
            long totalFolderSize = directory.GetDirectorySize();
            PathExtension[] files = directory.GetContent();
            long bytesFinished = 0;

            foreach (var path in files)
            {
                if (path is not FileExtension file)
                    continue;
                
                using var fileStream = File.Create(file.GetPath());

                progress?.Report(new DownloadProgress(
                    file.GetFileName(),
                    file.GetFileSize(PathType.Cluster),
                    0,
                    totalFolderSize,
                    bytesFinished
                ));

                DownloadFile(
                    file.GetPath(PathType.Cluster),
                    fileStream,
                    downloadedBytes =>
                    {
                        long totalDownloaded = bytesFinished + (long)downloadedBytes;

                        progress.Report(new DownloadProgress
                            (
                                file.GetFileName(PathType.Cluster),
                                file.GetFileSize(PathType.Cluster),
                                (long)downloadedBytes,
                                totalFolderSize,
                                totalDownloaded)
                        );
                    });
                bytesFinished += file.GetFileSize(PathType.Cluster);
            }
        }
        finally
        {
            _fileTransferService.Disconnect();
        }
    }
    
    private void BuildRecursiveDirs(string directoryPath)
    {
        string[] parts = directoryPath.Split('/');
        
        string path = "";
        
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
            {
                continue;
            }
            path += "/" + part;
            if (!_fileTransferService.FileClient.Exists(path))
            {
                Console.WriteLine($"Creating directory: {path}");
                _fileTransferService.FileClient.CreateDirectory(path);
            }
        }
        
    }
    
    public DirectoryExtension[] GetJobsOnCluster()
    {
        List<DirectoryExtension> pathsToJobs = new List<DirectoryExtension>();
    
        var cmd = _sshService.ConnectAndExecute(() =>
            _sshService.CommandClient.RunCommand("cd Rechnungen && find -maxdepth 3 -mindepth 3 -type d"));

        string fullString = cmd.Result;
        
        string[] splitString = fullString.Split(
            ["\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var path in splitString)
        {
            var relativePath = path.Substring(1, path.Length - 1);
            var newPathObject = new DirectoryExtension(relativePath, _sshService);
            pathsToJobs.Add(newPathObject);
        }
    
        return pathsToJobs.ToArray();
    }

    public string? GetJobId(PathObject directory)
    {
        _sshService.Connect();
        string fullName;
        string[] splitName;
        
        PathObject[] files = Dir(directory);

        foreach (var fileName in files)
        {
            if (fileName.ClusterPath.EndsWith(".log") ||
                fileName.ClusterPath.EndsWith(".lg") ||
                fileName.ClusterPath.EndsWith(".fchk"))
            {
                fullName = fileName.ClusterPath;
                splitName = fullName.Split('.');
                string jobId = splitName[1] + ".hpc-batch";
                _sshService.Disconnect();
                return jobId;
            }
        }
        _sshService.Disconnect();
        return null;
    }

    public JobState GetJobState(DirectoryExtension directory)
    {
        JobState jobState;
        var content = directory.GetContent(PathType.Cluster);

        if (content.Length == 0)
        {
            throw new FileNotFoundException("Directory is empty");
        }

        jobState = JobState.Queue;


        return jobState;
    }

    public string QStat()
    {
        var cfg = Config.Load();
        var username = cfg.ClusterUsername;

        var cmd = _sshService.ConnectAndExecute(() => 
            _sshService.CommandClient.RunCommand($"qstat -u {username}"));

        if (!string.IsNullOrWhiteSpace(cmd.Error))
        {
            var ex = new InvalidOperationException($"Error occurred while fetching job status: {cmd.Error}");
            Log.Error(ex, $"Error occurred while fetching job status: {cmd.Error}");
            throw ex ;
        }

        return cmd.Result;
    }
}
