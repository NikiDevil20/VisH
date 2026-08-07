using System.IO;
using VisH.Model.Configs;
using Renci.SshNet;

namespace VisH.Model.Hilbert;

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
            if (!FileClient.Exists(path))
            {
                Console.WriteLine($"Creating directory: {path}");
                FileClient.CreateDirectory(path);
            }
        }
        
    }
    
    public void UploadFiles(
        string[] localFilePaths,
        string[] remoteFilePaths,
        string remoteDirectory)
    {
        for (int i = 0; i < localFilePaths.Length; i++)
        {
            BuildRecursiveDirs(remoteDirectory);
            var fileStream = File.OpenRead(localFilePaths[i]);
            FileClient.UploadFile(fileStream, remoteFilePaths[i]);
        }
    }
}
