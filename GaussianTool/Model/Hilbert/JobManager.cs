using System.IO;
using GaussianTool.Model.Configs;

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
    
    public static bool NormalTermination(string dirPath, string logfileName)
    {
        var cmd = _sshService.CommandClient.RunCommand(
            $"cd {dirPath} && " +
            $"tail {logfileName}");
        return cmd.Result.Contains("Normal termination");
    }

    public static Dictionary<string, string> JobStatusAndId(string path)
    {
        bool logExists = false;
        bool qstatOutput = false;
        bool normalTermination = false;

        string jobId = "";
        string status = "Q";

        Dictionary<string, string> jobInfo = new Dictionary<string, string>();
        
        string[] files = Dir(path);
        
        foreach (string file in files)
        { 
           if (file.EndsWith(".log")) 
           {
               Console.WriteLine($"Processing log file: {file}");
               logExists = true;
               string logFile = file;
               string[] splitName = logFile.Split(".");
               jobId = splitName[1] + ".hpc-batch";

               normalTermination = NormalTermination(path, logFile);
               if (!normalTermination)
               {
                   var qstat = _sshService.CommandClient.RunCommand($"qstat {jobId}");
                   if (!string.IsNullOrWhiteSpace(qstat.Result))
                   {
                       qstatOutput = true;
                   }
               }
           }
        }
        
        if (logExists && qstatOutput)
        {
            status = "R";
        }
        else if (logExists && !qstatOutput)
        {
            status = "F";
        }
        if (normalTermination)
        {
            status = "S";
        }
        
        // if (string.IsNullOrEmpty(jobId))
        // {
        //     throw new FileNotFoundException("Job not found.");
        // }
        
        jobInfo["jobName"] = Path.GetFileName(path);
        jobInfo["jobId"] = jobId;
        jobInfo["status"] = status;
        return jobInfo;
        
    }

    public static string DeleteJob(string jobId)
    {
        var cmd = _sshService.CommandClient.RunCommand($"qdel {jobId}");
        return cmd.Result;
    }

    private static string[] Dir(string path)
    {
        var cmd = _sshService.CommandClient.RunCommand($"cd {path} && dir");
        string fullString = cmd.Result;
        string[] splitString = fullString.Split([" ", "\n", "\r", "\t"], StringSplitOptions.RemoveEmptyEntries);
        
        return splitString;
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
    
    private static string DownloadSpecificFile(string[] remoteFilePaths, string[] localFilePaths)
    {
        for (int i = 0; i < remoteFilePaths.Length; i++)
        {
            var fileStream = File.Create(localFilePaths[i]);
            _fileTransferService.FileClient.DownloadFile(remoteFilePaths[i], fileStream);
        }
        return "File downloaded successfully.";
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

    public static void DownloadFile(string ending, string remoteDirectory, string localDirectory)
    {
        string[] files = Dir(remoteDirectory);
        foreach (var file in files)
        {
            if (file.EndsWith(ending))
            {
                string remoteFilePath = remoteDirectory + $"/{file}";
                string localFilePath = localDirectory + $"/{file}";
                DownloadSpecificFile([remoteFilePath], [localFilePath]);
                return;
            }

            throw new FileNotFoundException($"Datei mit Endung {ending} in {string.Join(", ", files)} nicht gefunden.");
        }
    }
    
    public static string[] GetJobsOnCluster()
    {
        List<string> pathsToJobs = new List<string>();
        
        var cmd = _sshService.CommandClient.RunCommand("cd Rechnungen && find -maxdepth 3 -mindepth 3 -type d");
        string fullString = cmd.Result;
        string[] splitString = fullString.Split([" ", "\n", "\r", "\t"], StringSplitOptions.RemoveEmptyEntries);

        foreach (var path in splitString)
        {
            string newPath = "Rechnungen" + path.Substring(1, path.Length - 1);
            pathsToJobs.Add(newPath);
        }
        
        return pathsToJobs.ToArray();
    }

    public static string? GetJobId(string directory)
    {
        _sshService.Connect();
        string fullName;
        string[] splitName;
        
        string[] files = Dir(directory);

        foreach (var fileName in files)
        {
            if (fileName.EndsWith(".log") ||
                fileName.EndsWith(".lg") ||
                fileName.EndsWith(".fchk"))
            {
                fullName = fileName;
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