using GaussianTool.Model.Configs;
using Renci.SshNet;

namespace GaussianTool.Model.Hilbert;

public class SshService
{
    public SshClient CommandClient { get; set; }

    public SshService()
    {
        Config cfg = Config.Load();
        
        var key = new PrivateKeyFile(cfg.SshKeyPath);
        
        CommandClient = new SshClient(cfg.Cluster, cfg.ClusterUsername, key);
    }
    
    public void Connect()
    {
        CommandClient.Connect();
    }
    
    public void Disconnect()
    {
        CommandClient.Disconnect();
    }

    public string GetClusterDestinationType(string path)
    {
        string escapedPath = path.Replace("\"", "\\\"");

        string command =
            $"[ -d \"{escapedPath}\" ] && echo D || [ -f \"{escapedPath}\" ] && echo F || echo N";

        var cmd = CommandClient.RunCommand(command);
        return cmd.Result.Trim();
    }
}