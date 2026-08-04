using VisH.Model.Enums;
using VisH.Model.Hilbert;

namespace VisH.Model.FileHandling;

public class FileHandler
{
    public DownloadManager DownloadManager { get; set; }
    public UploadManager UploadManager { get; set; }

    public event Action? ClusterChanged;
    public FileHandler()
    {
        DownloadManager = new DownloadManager();
        UploadManager = new UploadManager();
    }
    
    public async Task Download(PathObject path)
    {
        await DownloadManager.DownloadFolder(path);
        ClusterChanged?.Invoke();
    }

    public string Upload(Calculation calculation)
    {
        string jobId = UploadManager.Run(calculation);
        ClusterChanged?.Invoke();
        return jobId;
    }

}
