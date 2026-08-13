using VisH.Model.CalculationUtils;
using VisH.Model.CalculationObject;
using VisH.Model.CalculationUtils;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class FileHandler
{
    public DownloadManager DownloadManager { get; set; }
    public UploadManager UploadManager { get; set; }
    
    private SshService _sshService;
    private FileTransferService _fileTransferService;
    private JobStarter _jobStarter;

    public event Action? ClusterChanged;
    
    public FileHandler(SshService sshService, FileTransferService fileTransferService, JobStarter jobStarter)
    {
        _sshService = sshService;
        _fileTransferService = fileTransferService;
        _jobStarter = jobStarter;
        DownloadManager = new DownloadManager();
        UploadManager = new UploadManager();
    }
    
    public async Task Download(DirectoryExtension directory)
    {
        await DownloadManager.DownloadFolder(directory);
        ClusterChanged?.Invoke();
    }
    
    public string[] Upload(Calculation[] calculations)
    {
        string[] clusterDirectories = new string[calculations.Length];
        
        Runner runner = new Runner(_jobStarter, _sshService, _fileTransferService);

        for (int i = 0; i < calculations.Length; i++)
        {
            calculations[i].WriteFiles();
            clusterDirectories[i] = calculations[i].Paths.RelativeDirectory.GetPath(PathType.Cluster);
            var gaussianInputFile = calculations[i].Paths.GaussianInputFile;
            var gstartFile = calculations[i].Paths.GstartFile;
            
            Console.WriteLine("FileHandler");
            Console.WriteLine(gaussianInputFile.GetPath(PathType.Cluster));
            Console.WriteLine(gstartFile.GetPath(PathType.Cluster));
            
            runner.UploadJob(
                [gaussianInputFile.GetPath(), gstartFile.GetPath()],
                [gaussianInputFile.GetPath(PathType.Cluster), gstartFile.GetPath(PathType.Cluster)],
                clusterDirectories[i]);
            
        }

        var jobIds = runner.SubmitJobs(clusterDirectories);
        
        ClusterChanged?.Invoke();
        return jobIds;
    }

}
