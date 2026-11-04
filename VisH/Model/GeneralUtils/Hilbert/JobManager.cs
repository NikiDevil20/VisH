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
    
    private void DownloadFile(string path, Stream fileStream, Action<ulong> downloadCallback)
    {
        _fileTransferService.FileClient.DownloadFile(path, fileStream, downloadCallback);
    }
    
    public bool DownloadFolder(
        DirectoryExtension directory,
        IProgress<DownloadProgress>? progress = null)
    {
        _fileTransferService.Connect();
        try
        {
            long totalFolderSize = directory.GetDirectorySize(PathType.Cluster);
            PathExtension[] files = GetFilesRecursively(directory);
            long bytesFinished = 0;

            var clusterFiles = files.OfType<FileExtension>().ToList();
            var fileSizes = new Dictionary<string, long>();

            foreach (var file in clusterFiles)
            {
                var localFilePath = file.GetPath(PathType.Local);
                var localDirectory = Path.GetDirectoryName(localFilePath);
                if (!string.IsNullOrEmpty(localDirectory) && !Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                var clusterFilePath = file.GetPath(PathType.Cluster);
                var fileName = file.GetFileName(PathType.Cluster);
                long currentFileSize;
                try
                {
                    currentFileSize = _fileTransferService.FileClient.GetAttributes(clusterFilePath).Size;
                }
                catch
                {
                    currentFileSize = file.GetFileSize(PathType.Cluster);
                }

                fileSizes[clusterFilePath] = currentFileSize;

                using var fileStream = File.Create(localFilePath);

                progress?.Report(new DownloadProgress(
                    fileName,
                    currentFileSize,
                    0,
                    totalFolderSize,
                    bytesFinished
                ));

                DownloadFile(
                    clusterFilePath,
                    fileStream,
                    downloadedBytes =>
                    {
                        long totalDownloaded = bytesFinished + (long)downloadedBytes;

                        progress?.Report(new DownloadProgress(
                            fileName,
                            currentFileSize,
                            (long)downloadedBytes,
                            totalFolderSize,
                            totalDownloaded
                        ));
                    });
                bytesFinished += currentFileSize;

                progress?.Report(new DownloadProgress(
                    fileName,
                    currentFileSize,
                    currentFileSize,
                    totalFolderSize,
                    bytesFinished
                ));
            }

            bool allFilesDownloaded = clusterFiles.Count > 0 && clusterFiles.All(file =>
            {
                var localPath = file.GetPath(PathType.Local);
                if (!File.Exists(localPath))
                    return false;

                var clusterPath = file.GetPath(PathType.Cluster);
                if (fileSizes.TryGetValue(clusterPath, out var expectedSize))
                {
                    return new FileInfo(localPath).Length == expectedSize;
                }

                return true;
            });

            if (allFilesDownloaded)
            {
                foreach (var file in clusterFiles)
                {
                    var clusterFilePath = file.GetPath(PathType.Cluster);
                    if (_fileTransferService.FileClient.Exists(clusterFilePath))
                    {
                        _fileTransferService.FileClient.DeleteFile(clusterFilePath);
                    }
                }

                DeleteRemoteDirectory(directory);

                return true;
            }

            return false;
        }
        finally
        {
            _fileTransferService.Disconnect();
        }
    }

    private static PathExtension[] GetFilesRecursively(DirectoryExtension directory)
    {
        var files = new List<PathExtension>();
        foreach (var entry in directory.GetContent(PathType.Cluster))
        {
            if (entry is FileExtension)
            {
                files.Add(entry);
            }
            else if (entry is DirectoryExtension childDirectory)
            {
                files.AddRange(GetFilesRecursively(childDirectory));
            }
        }

        return files.ToArray();
    }

    private void DeleteRemoteDirectory(DirectoryExtension directory)
    {
        foreach (var entry in directory.GetContent(PathType.Cluster))
        {
            if (entry is FileExtension file)
            {
                if (_fileTransferService.FileClient.Exists(file.GetPath(PathType.Cluster)))
                {
                    _fileTransferService.FileClient.DeleteFile(file.GetPath(PathType.Cluster));
                }
            }
            else if (entry is DirectoryExtension childDirectory)
            {
                DeleteRemoteDirectory(childDirectory);
            }
        }

        var directoryPath = directory.GetPath(PathType.Cluster);
        if (_fileTransferService.FileClient.Exists(directoryPath))
        {
            _fileTransferService.FileClient.DeleteDirectory(directoryPath);
        }
    }
    
    public DirectoryExtension[] GetJobsOnCluster()
    {
        List<DirectoryExtension> pathsToJobs = new List<DirectoryExtension>();
    
        var cmd = _sshService.ConnectAndExecute(() =>
            _sshService.CommandClient.RunCommand(
                "cd Rechnungen && find . -mindepth 3 -type d"));

        string fullString = cmd.Result;
        
        string[] splitString = fullString.Split(
            ["\n", "\r"],
            StringSplitOptions.RemoveEmptyEntries);
        
        foreach (var path in splitString)
        {
            var relativePath = path.Trim();
            if (relativePath.StartsWith("./", StringComparison.Ordinal))
            {
                relativePath = relativePath[2..];
            }
            var newPathObject = new DirectoryExtension(relativePath, _sshService);
            pathsToJobs.Add(newPathObject);
        }
    
        return pathsToJobs.ToArray();
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
