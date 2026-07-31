using GaussianTool.Model.FileHandling;

namespace GaussianTool.ViewModel;

public class MainWindowViewModel
{
    public StatusBarViewModel StatusViewModel { get; }
    public OverviewViewModel OverviewViewModel { get; }
    public MenuBarViewModel MenuBarViewModel { get; }
    public DownloadManager DownloadManager { get; }
    
    public FileHandler FileHandler { get; set; }

    public MainWindowViewModel()
    {
        FileHandler = new FileHandler();
        
        StatusViewModel = new StatusBarViewModel(FileHandler);
        
        OverviewViewModel = new OverviewViewModel();
        
        MenuBarViewModel = new MenuBarViewModel(FileHandler);
        
        DownloadManager = FileHandler.DownloadManager;
    }
}