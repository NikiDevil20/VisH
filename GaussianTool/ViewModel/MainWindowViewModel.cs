using GaussianTool.Model.FileHandling;

namespace GaussianTool.ViewModel;

public class MainWindowViewModel
{
    public StatusBarViewModel StatusViewModel { get; }
    public OverviewViewModel OverviewViewModel { get; }
    public DownloadManager DownloadManager { get; }

    public MainWindowViewModel()
    {

        var downloadManager = new DownloadManager();
        StatusViewModel = new StatusBarViewModel(downloadManager);
        
        OverviewViewModel = new OverviewViewModel();

        DownloadManager = downloadManager;
    }
}