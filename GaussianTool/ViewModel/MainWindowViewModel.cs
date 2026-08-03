using GaussianTool.Model;
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
        PythonBridge pythonBridge = new PythonBridge();
        LogFileAnalyzer logFileAnalyzer = new LogFileAnalyzer(pythonBridge);
        
        StatusViewModel = new StatusBarViewModel(FileHandler);
        
        OverviewViewModel = new OverviewViewModel(logFileAnalyzer);
        
        MenuBarViewModel = new MenuBarViewModel(FileHandler);
        
        DownloadManager = FileHandler.DownloadManager;
    }
}