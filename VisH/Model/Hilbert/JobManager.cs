using System.IO;
using VisH.Model.Configs;
using VisH.Model.Enums;
using VisH.Model.FileHandling;
using VisH.Model.PostRun;

namespace VisH.Model.Hilbert;

public static class JobManager
{
    private static SshService _sshService = new SshService();
    private static FileTransferService _fileTransferService = new FileTransferService();
    
    public static void Connect()
    {
        _sshService.Connect();
    }
    
    public static void Disconnect()
    {
        _sshService.Disconnect();
    }
    public static string StartJob(string clusterDirectory, string[] localFilePaths, string[] clusterFilesPaths)
    {
        _fileTransferService.Connect();
        _sshService.Connect();
        Console.WriteLine(clusterDirectory);
        BuildRecursiveDirs(clusterDirectory);
        
        UploadFiles(localFilePaths, clusterDirectory, clusterFilesPaths);
        
        var cmd = _sshService.CommandClient.RunCommand($"cd {clusterDirectory} && qsub gstart");
        
        _fileTransferService.Disconnect();
        _sshService.Disconnect();
        return cmd.Result;
    }
    
    private static bool NormalTermination(PathObject path)
    {
        var cmd = _sshService.CommandClient.RunCommand(
            $"cd {path.ClusterFolder} && " +
            $"tail {path.Filename}");
        return cmd.Result.Contains("Normal termination");
    }
    
    

    public static Dictionary<string, string> JobStatusAndId(PathObject path)
    {
        bool logExists = false;
        bool qstatOutput = false;
        bool normalTermination = false;

        string? jobId = null;
        State status = State.Queue;

        Dictionary<string, string> jobInfo = new Dictionary<string, string>();
        
        if (path.DestinationType != PathType.Directory)
            throw new ArgumentException("Path must be a directory.");

        if (path.FolderContent == null || path.FolderContent.Length == 0)
            throw new FileNotFoundException("Directory is empty.");
        
        PathObject[] files = path.GetFolderContent();
        
        foreach (var file in files)
        {
           if (file.ClusterPath.EndsWith(".log")) 
           {
               logExists = true;
               jobId = GaussianRegex.MatchString(file.ClusterPath, GaussianRegex.JobIdLogFile);

               normalTermination = NormalTermination(file);
               if (!normalTermination)
               {
                   if (jobId != null)
                   {
                       var qstat = _sshService.CommandClient.RunCommand($"qstat {jobId}");
                       if (!string.IsNullOrWhiteSpace(qstat.Result))
                       {
                           qstatOutput = true;
                       }
                   }
               }
           }
        }
        if (logExists && qstatOutput)
        {
            status = State.Running;
        }
        else if (logExists && !qstatOutput)
        {
            status = State.Failed;
        }
        if (normalTermination)
        {
            status = State.Successful;
        }
        
        jobInfo["jobName"] = Path.GetFileName(path.ClusterPath);
        jobInfo["jobId"] = jobId;
        jobInfo["status"] = status.ToString();
        return jobInfo;
        
    }

    public static string DeleteJob(string jobId)
    {
        var cmd = _sshService.CommandClient.RunCommand($"qdel {jobId}");
        return cmd.Result;
    }

    public static PathObject[] Dir(PathObject path, PathType? onlyListOneType = null)
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
    
    public static string UploadFiles(string[] localFilePaths, string remoteDirectory, string[] remoteFilePaths)
    {
        for (int i = 0; i < localFilePaths.Length; i++)
        {
            BuildRecursiveDirs(remoteDirectory);
            var fileStream = File.OpenRead(localFilePaths[i]);
            _fileTransferService.FileClient.UploadFile(fileStream, remoteFilePaths[i]);
        }
        return "File uploaded successfully.";
    }
    
    public static ulong FileSize(PathObject path)
    {
        try
        {
            _fileTransferService.Connect();
            if (path.DestinationType == PathType.Directory)
            {
                return 0;
            }

            var attributes = _fileTransferService.FileClient.GetAttributes(path.ClusterPath);
            return (ulong)attributes.Size;
        }
        finally
        {
            _fileTransferService.Disconnect();
        }
    }
    
    private static void DownloadFile(string path, Stream fileStream, Action<ulong> downloadCallback)
    {
        _fileTransferService.FileClient.DownloadFile(path, fileStream, downloadCallback);
    }
    
    public static void DownloadFolder(
        PathObject path,
        IProgress<DownloadProgress> progress)
    {
        _fileTransferService.Connect();
        try
        {
            ulong? totalFolderSize = path.Size;
            ulong bytesFinished = 0;

            foreach (var file in path.FolderContent)
            {
                using var fileStream = File.Create(file.WindowsPath);

                progress?.Report(new DownloadProgress(
                    file.Filename,
                    file.Size ?? 0,
                    0,
                    totalFolderSize ?? 0,
                    bytesFinished
                ));

                DownloadFile(
                    file.ClusterPath,
                    fileStream,
                    downloadedBytes =>
                    {
                        ulong totalDownloaded = bytesFinished + downloadedBytes;

                        progress.Report(new DownloadProgress
                            (
                                file.Filename,
                                file.Size ?? 0,
                                downloadedBytes,
                                totalFolderSize ?? 0,
                                totalDownloaded)
                        );
                    });
                bytesFinished += file.Size ?? 0;
            }
        }
        finally
        {
            _fileTransferService.Disconnect();
        }
    }
    
    private static void BuildRecursiveDirs(string directoryPath)
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

    
    public static PathObject[] GetJobsOnCluster()
    {
        List<PathObject> pathsToJobs = new List<PathObject>();
        
        var cmd = _sshService.CommandClient.RunCommand("cd Rechnungen && find -maxdepth 3 -mindepth 3 -type d");
        string fullString = cmd.Result;
        string[] splitString = fullString.Split(
            ["\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in splitString)
        {
            string newPath = "Rechnungen" + path.Substring(1, path.Length - 1);
            var newPathObject = new PathObject(newPath, true);
            pathsToJobs.Add(newPathObject);
        }
        
        return pathsToJobs.ToArray();
    }

    public static string? GetJobId(PathObject directory)
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
}
