using Serilog;

namespace VisH.Model.GeneralUtils.Hilbert;

public class JobStarter
{
    private SshService _sshService;
    private FileTransferService _fileTransferService;
    
    public JobStarter(SshService sshService, FileTransferService fileTransferService)
    {
        _sshService = sshService;
        _fileTransferService = fileTransferService;
    }
    
    /// <summary>
    /// Runs the jobs on the cluster.
    /// </summary>
    /// <param name="calculationClusterDirectories">Paths to all cluster directories with jobs to start.</param>
    /// <returns>
    /// Returns the job IDs of all submitted jobs. <br />
    /// Format: __start_N__ jobID __end_N__
    /// </returns>
    /// <exception cref="Exception">Throws a general exception if an error is thrown by the cluster operation
    /// and forwards it.</exception>
    public string Run(string[] calculationClusterDirectories)
    {
        var fullCommand = ChainCommand("qsub gstart", calculationClusterDirectories);
        
        var cmd = _sshService.CommandClient.RunCommand(fullCommand);
        
        if (cmd.Error != "")
        {
            var ex = new Exception($"Error occurred while running job: {cmd.Error}");
            Log.Error(ex, "Error occurred while running job");
        }

        return cmd.Result;
    }

    // public void Upload(
    //     string[] calculationLocalDirectories,
    //     string[] remoteFilePaths,
    //     string remoteDirectory)
    // {
    //     _fileTransferService.UploadFiles(
    //         calculationLocalDirectories,
    //         remoteFilePaths,
    //         remoteDirectory);
    // }

    private string ChainCommand(string command, string[] paths)
    {
        string fullCommand;
        string[] formattedCommands = new string[paths.Length];

        for (int i = 0; i < paths.Length; i++)
        {
            formattedCommands[i] = $"(cd \"{paths[i]}\" && echo __start_{i}__ && {command} && echo __end_{i}__)";
        }
        
        fullCommand = string.Join("; ", formattedCommands);
        return fullCommand;
    }
    
    
}