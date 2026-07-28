using System.IO;
using GaussianTool.Model.Configs;
using GaussianTool.Model.Enums;
using GaussianTool.Model.FileHandling;
using GaussianTool.Model.PostRun;

namespace GaussianTool.Model.Hilbert;

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
            $"cd {path.Foldername} && " +
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
        
        PathObject[] files = path.FolderContent;
        
        foreach (var file in files)
        {
           if (file.ClusterPath.EndsWith(".log")) 
           {
               logExists = true;
               
               jobId = GaussianRegex.MatchString(file.ClusterPath, @"\.(\d+\.hpc-batch)\.log$");

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
        
        // if (string.IsNullOrEmpty(jobId))
        // {
        //     throw new FileNotFoundException("Job not found.");
        // }
        
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
    
    private static string DownloadSpecificFile(PathObject[] remoteFilePaths, PathObject[] localFilePaths)
    {
        for (int i = 0; i < remoteFilePaths.Length; i++)
        {
            var fileStream = File.Create(localFilePaths[i].ClusterPath);
            _fileTransferService.FileClient.DownloadFile(remoteFilePaths[i].ClusterPath, fileStream);
        }
        return "Success";
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

    public static void DownloadFolder(PathObject path)
    {
        PathObject[] files = Dir(path);
        var remoteFiles = new List<PathObject>();
        var localFiles = new List<PathObject>();
        foreach (var file in files)
        {
            PathObject remoteFile = path.Join(file.ClusterPath);
            PathObject localFile = path.Join(file.ClusterPath);
            remoteFiles.Add(remoteFile);
            localFiles.Add(localFile);
        }
        DownloadSpecificFile(remoteFiles.ToArray(), localFiles.ToArray());
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