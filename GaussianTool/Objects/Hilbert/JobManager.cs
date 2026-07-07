using System.IO;

namespace GaussianTool.Objects.Hilbert;

public static class JobManager
{
    private static SshService _sshService = new SshService();
    private static FileTransferService _fileTransferService = new FileTransferService();

    public static string StartJob(string clusterGstartPath)
    {
        throw new NotImplementedException();
    }
    
    public static string JobStatus(string? jobId)
    {
        throw new NotImplementedException();
    }

    public static string DeleteJob(string jobId)
    {
        throw new NotImplementedException();
    }

    private static string[] ListFiles(string remoteDirectory)
    {
        
        _sshService.Connect();
        var cmd = _sshService.CommandClient.RunCommand($"cd {remoteDirectory} && dir");
        string msg = cmd.Result;
        
        
        _sshService.Disconnect();
        return msg.Split(' ');
    }
    
    public static string UploadFiles(string[] localFilePaths, string remoteDirectory, string[] remoteFilePaths)
    {
        _fileTransferService.Connect();
        for (int i = 0; i < localFilePaths.Length; i++)
        {
            BuildRecursiveDirs(remoteDirectory);
            var fileStream = File.OpenRead(localFilePaths[i]);
            _fileTransferService.FileClient.UploadFile(fileStream, remoteFilePaths[i]);
        }
        _fileTransferService.Disconnect();
        return "File uploaded successfully.";
    }
    
    private static string DownloadSpecificFile(string[] remoteFilePaths, string[] localFilePaths)
    {
        _fileTransferService.Connect();
        for (int i = 0; i < remoteFilePaths.Length; i++)
        {
            var fileStream = File.Create(localFilePaths[i]);
            _fileTransferService.FileClient.DownloadFile(remoteFilePaths[i], fileStream);
        }
        _fileTransferService.Disconnect();
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
        string[] files = ListFiles(remoteDirectory);
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
}