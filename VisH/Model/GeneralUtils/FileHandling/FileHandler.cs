using System.IO;
using VisH.Model.CalculationUtils;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class FileHandler
{
    public DownloadManager DownloadManager { get; set; }
    
    private SshService _sshService;
    private FileTransferService _fileTransferService;
    private JobStarter _jobStarter;

    public event Action? ClusterChanged;
    
    public FileHandler(
        SshService sshService,
        FileTransferService fileTransferService,
        JobStarter jobStarter,
        JobManager jobManager)
    {
        _sshService = sshService;
        _fileTransferService = fileTransferService;
        _jobStarter = jobStarter;
        DownloadManager = new DownloadManager(jobManager);
    }
    
    public async Task<bool> Download(DirectoryExtension directory)
    {
        bool success = await DownloadManager.DownloadFolder(directory);
        ClusterChanged?.Invoke();
        return success;
    }
    
    public string[] Upload(
        Calculation[] calculations,
        Action<Calculation>? beforeSubmit = null)
    {
        string[] clusterDirectories = new string[calculations.Length];
        
        Runner runner = new Runner(_jobStarter, _sshService, _fileTransferService);

        for (int i = 0; i < calculations.Length; i++)
        {
            calculations[i].WriteFiles();
            clusterDirectories[i] = calculations[i].Paths.RelativeDirectory.GetPath(PathType.Cluster);
            var gaussianInputFile = calculations[i].Paths.GaussianInputFile;
            var gstartFile = calculations[i].Paths.GstartFile;
            
            runner.UploadJob(
                [gaussianInputFile.GetPath(), gstartFile.GetPath()],
                [gaussianInputFile.GetPath(PathType.Cluster), gstartFile.GetPath(PathType.Cluster)],
                clusterDirectories[i]);
            beforeSubmit?.Invoke(calculations[i]);
            
        }

        var jobIds = runner.SubmitJobs(clusterDirectories);
        
        ClusterChanged?.Invoke();
        return jobIds;
    }

    public void CopyCheckpoint(FileExtension source, FileExtension destination)
    {
        _fileTransferService.ConnectAndExecute(() =>
        {
            string clusterSource = source.GetPath(PathType.Cluster);
            string localSource = source.GetPath(PathType.Local);
            string clusterDestination = destination.GetPath(PathType.Cluster);

            if (_fileTransferService.RemoteFileExists(clusterSource))
            {
                _fileTransferService.CopyFile(clusterSource, clusterDestination);
                return;
            }

            if (File.Exists(localSource))
            {
                _fileTransferService.UploadLocalFile(localSource, clusterDestination);
                return;
            }

            throw new FileNotFoundException(
                $"The checkpoint file for job '{source.GetDirectory(PathType.Local)}' " +
                "was not found on the cluster or locally.",
                localSource);
        });
    }

}
