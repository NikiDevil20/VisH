using GaussianTool.Objects.Configs;
using Renci.SshNet;

namespace GaussianTool.Objects.Hilbert;

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
}