using VisH.Model;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.ViewModel;

public class MainWindowViewModel
{
    public StatusBarViewModel StatusViewModel { get; }
    public OverviewViewModel OverviewViewModel { get; }
    public MenuBarViewModel MenuBarViewModel { get; }
    public DownloadManager DownloadManager { get; }
    
    public FileHandler FileHandler { get; set; }

    public MainWindowViewModel(SshService sshService)
    {
        var fileTransferService = new FileTransferService();
        var jobManager = new JobManager(sshService, fileTransferService);
        var jobStarter = new JobStarter(sshService, fileTransferService);
        
        
        FileHandler = new FileHandler(sshService, fileTransferService, jobStarter, jobManager);
        PythonBridge pythonBridge = new PythonBridge();
        LogFileAnalyzer logFileAnalyzer = new LogFileAnalyzer(pythonBridge);
        
        var jobFinder = new JobFinder(sshService, jobManager);
        StatusViewModel = new StatusBarViewModel(FileHandler, jobManager, jobFinder, sshService);
        
        OverviewViewModel = new OverviewViewModel(logFileAnalyzer, FileHandler, sshService, jobManager);
        
        MenuBarViewModel = new MenuBarViewModel(FileHandler, sshService, jobFinder);
        
        DownloadManager = FileHandler.DownloadManager;
    }
}
