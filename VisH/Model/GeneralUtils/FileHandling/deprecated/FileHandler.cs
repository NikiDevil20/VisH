using VisH.Model.Enums;

namespace VisH.Model.GeneralUtils.FileHandling.deprecated;

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
    
    // public async Task Download(PathObject path)
    // {
    //     await DownloadManager.DownloadFolder(path);
    //     ClusterChanged?.Invoke();
    // }
    //
    // public string Upload(Calculation.CalculationObject.Calculation calculation)
    // {
    //     string jobId = UploadManager.Run(calculation);
    //     ClusterChanged?.Invoke();
    //     return jobId;
    // }

}
