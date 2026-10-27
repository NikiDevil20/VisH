using System.IO;
using Renci.SshNet;

namespace VisH.Model.GeneralUtils.Hilbert;

public class FileTransferService
{
    public SftpClient FileClient { get; set; }
    
    public FileTransferService()
    {
        Config cfg = Config.Load();
        
        var key = new PrivateKeyFile(cfg.SshKeyPath);
        
        FileClient = new SftpClient(cfg.Storage, cfg.ClusterUsername, key);
    }

    public void Connect()
    {
        FileClient.Connect();
    }

    public void Disconnect()
    {
        FileClient.Disconnect();
    }
    
    private static string NormalizeRemotePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        var normalized = path.Replace('\\', '/').Trim();
        while (normalized.StartsWith('/'))
        {
            normalized = normalized[1..];
        }

        return normalized.Trim('/');
    }

    private void BuildRecursiveDirs(string directoryPath)
    {
        var normalizedPath = NormalizeRemotePath(directoryPath);
        if (string.IsNullOrWhiteSpace(normalizedPath))
        {
            return;
        }

        var parts = normalizedPath.Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        string path = string.Empty;

        foreach (var part in parts)
        {
            path = string.IsNullOrEmpty(path) ? part : $"{path}/{part}";
            if (!FileClient.Exists(path))
            { 
                FileClient.CreateDirectory(path);
            }
        }
    }
    
    public void UploadFiles(
        string[] localFilePaths,
        string[] remoteFilePaths,
        string remoteDirectory)
    {
        var normalizedRemoteDirectory = NormalizeRemotePath(remoteDirectory);
        var normalizedRemotePaths = remoteFilePaths
            .Select(path => NormalizeRemotePath(path))
            .ToArray();

        for (int i = 0; i < localFilePaths.Length; i++)
        {
            BuildRecursiveDirs(normalizedRemoteDirectory);
            var fileStream = File.OpenRead(localFilePaths[i]);
            Console.WriteLine(normalizedRemotePaths[i]);
            FileClient.UploadFile(fileStream, normalizedRemotePaths[i]);
        }
    }

    public void CopyFile(string sourcePath, string destinationPath)
    {
        var source = NormalizeRemotePath(sourcePath);
        var destination = NormalizeRemotePath(destinationPath);
        var destinationDirectory = Path.GetDirectoryName(destination)?.Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(source) || string.IsNullOrWhiteSpace(destination) ||
            string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException("Source and destination paths are required.");
        }

        BuildRecursiveDirs(destinationDirectory);

        using var buffer = new MemoryStream();
        FileClient.DownloadFile(source, buffer);
        buffer.Position = 0;
        FileClient.UploadFile(buffer, destination, true);
    }

    public bool RemoteFileExists(string path) =>
        FileClient.Exists(NormalizeRemotePath(path));

    public void UploadLocalFile(string localPath, string remotePath)
    {
        var destination = NormalizeRemotePath(remotePath);
        var destinationDirectory = Path.GetDirectoryName(destination)?.Replace('\\', '/');

        if (string.IsNullOrWhiteSpace(destination) ||
            string.IsNullOrWhiteSpace(destinationDirectory))
        {
            throw new ArgumentException("Local and remote paths are required.");
        }

        if (!File.Exists(localPath))
        {
            throw new FileNotFoundException("The local source file does not exist.", localPath);
        }

        BuildRecursiveDirs(destinationDirectory);
        using var fileStream = File.OpenRead(localPath);
        FileClient.UploadFile(fileStream, destination, true);
    }
    
    /// <summary>
    /// Connects to the file transfer client if it is not already connected and executes the specified operation.
    /// Suitable for any return types.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <returns>The result of the operation</returns>
    public TResult ConnectAndExecute<TResult>(Func<TResult> operation)
    {
        var connectedHere = false;
        
        try
        {
            if (!FileClient.IsConnected)
            {
                Connect();
                connectedHere = true;
            }
            
            var result = operation();
            return result;
        }
        finally
        {
            if (connectedHere && FileClient.IsConnected)
            {
                Disconnect();
            }
        }
    }
    
    /// <summary>
    /// Connects to the file transfer client if it is not already connected and executes the specified operation.
    /// Suitable for operations that do not return a value.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    public void ConnectAndExecute(Action operation)
    {
        var connectedHere = false;
        
        try
        {
            if (!FileClient.IsConnected)
            {
                Connect();
                connectedHere = true;
            }
            
            operation();
        }
        finally
        {
            if (connectedHere && FileClient.IsConnected)
            {
                Disconnect();
            }
        }
    }
}
