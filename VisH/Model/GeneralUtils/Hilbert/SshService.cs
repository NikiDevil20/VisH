using Renci.SshNet;

namespace VisH.Model.GeneralUtils.Hilbert;

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
            $"if [ -d \"{escapedPath}\" ]; then " +
            "echo D; " +
            $"elif [ -f \"{escapedPath}\" ]; then " +
            "echo F; " +
            "else " +
            "echo N; " +
            "fi";
    
        var cmd = CommandClient.RunCommand(command);
        return cmd.Result.Trim();
    }
    
    public string[] GetClusterContent(string path)
    {
        string command = $"cd {path} && ls -Ap";

        var cmd = CommandClient.RunCommand(command);
        var result = cmd.Result.Trim();

        if (string.IsNullOrEmpty(result))
        {
            return Array.Empty<string>();
        }

        var splitLines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var fullPaths = splitLines.Select(line => $"{path}/{line}");

        return fullPaths.ToArray();
    }
    
    public long? GetClusterDirectorySize(string path)
    {
        string command = $"du -sb {path} | cut -f1";
        var cmd = CommandClient.RunCommand(command);
        var result = cmd.Result.Trim();

        if (string.IsNullOrEmpty(result) || !long.TryParse(result, out long size))
        {
            return null;
        }

        return size;
    }
    
    public long? GetClusterFileSize(string path)
    {
        string command = $"stat -c %s {path}";
        var cmd = CommandClient.RunCommand(command);
        var result = cmd.Result.Trim();

        if (string.IsNullOrEmpty(result) || !long.TryParse(result, out long size))
        {
            return null;
        }

        return size;
    }

    /// <summary>
    /// Connects to the SSH client if it is not already connected and executes the specified operation.
    /// Suitable for any return types.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    /// <returns>The result of the operation</returns>
    public TResult ConnectAndExecute<TResult>(Func<TResult> operation)
    {
        var connectedHere = false;
        
        try
        {
            if (!CommandClient.IsConnected)
            {
                Connect();
                connectedHere = true;
            }
            
            var result = operation();
            return result;
        }
        finally
        {
            if (connectedHere && CommandClient.IsConnected)
            {
                Disconnect();
            }
        }
    }
    
    /// <summary>
    /// Connects to the SSH client if it is not already connected and executes the specified operation.
    /// Suitable for operations that do not return a value.
    /// </summary>
    /// <param name="operation">The operation to execute</param>
    public void ConnectAndExecute(Action operation)
    {
        var connectedHere = false;
        
        try
        {
            if (!CommandClient.IsConnected)
            {
                Connect();
                connectedHere = true;
            }
            
            operation();
        }
        finally
        {
            if (connectedHere && CommandClient.IsConnected)
            {
                Disconnect();
            }
        }
    }
    
    
    
}
