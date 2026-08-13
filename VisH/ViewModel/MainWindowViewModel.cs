using VisH.Model;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.ViewModel;

public class MainWindowViewModel
{
    public StatusBarViewModel StatusViewModel { get; }
    public OverviewViewModel OverviewViewModel { get; }
    public MenuBarViewModel MenuBarViewModel { get; }
    public DownloadManager DownloadManager { get; }
    
    public FileHandler FileHandler { get; set; }

    public MainWindowViewModel()
    {
        var sshService = new SshService();
        var fileTransferService = new FileTransferService();
        var jobStarter = new JobStarter(sshService, fileTransferService);
        
        
        FileHandler = new FileHandler(sshService, fileTransferService, jobStarter);
        PythonBridge pythonBridge = new PythonBridge();
        LogFileAnalyzer logFileAnalyzer = new LogFileAnalyzer(pythonBridge);
        
        StatusViewModel = new StatusBarViewModel(FileHandler);
        
        OverviewViewModel = new OverviewViewModel(logFileAnalyzer, FileHandler);
        
        MenuBarViewModel = new MenuBarViewModel(FileHandler);
        
        DownloadManager = FileHandler.DownloadManager;
    }
}
