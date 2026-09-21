using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using VisH.Model;
using VisH.Model.CalculationObject;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.FileHandling;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.PostRun;

namespace VisH.ViewModel;

public class StatusBarViewModel : ViewModelBase
{
    private FileHandler _fileHandler;
    private readonly JobManager _jobManager;
    private readonly JobFinder _jobFinder;
    private readonly SshService _sshService;
    public ObservableCollection<CalcStatus> JobsOnCluster { get; } = new();
    private CalcStatus? _selectedCalcstatus;
    public CalcStatus? SelectedCalcstatus
    {
        get => _selectedCalcstatus;
        set
        {
            if (_selectedCalcstatus != value)
            {
                _selectedCalcstatus = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    
    public RelayCommand RefreshCommand => new RelayCommand(execute => RefreshStatus());
    
    public RelayCommand DownloadCommand => new RelayCommand(
        execute => DownloadSelection(), canExecute => CanDownload());
    
    private bool _changeToClusterWasMade { get; set; }
    
    
    public StatusBarViewModel(FileHandler fileHandler, JobManager jobManager, JobFinder jobFinder, SshService sshService)
    {
        _fileHandler = fileHandler;
        _jobManager = jobManager;
        _jobFinder = jobFinder;
        _sshService = sshService;
        _changeToClusterWasMade = true;
        _fileHandler.ClusterChanged += () => RefreshJobList();
    }

    public void RefreshJobList()
    {
        try
        {
            _jobManager.Connect();
            // PathObject[] jobPaths = JobManager.GetJobsOnCluster();
            // _pathsOnCluster = jobPaths.ToArray();
            // TODO
            
        }
        catch (Renci.SshNet.Common.SshAuthenticationException e)
        {
            var cfg = Config.Load();
            string userName = cfg.ClusterUsername;
            string keyPath = cfg.SshKeyPath;
            MessageBox.Show(
                $"Authentication with username '{userName}' and key '{keyPath}' failed.\n" +
                $"Please check your credentials.",
                "Authentication Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch (System.Net.Sockets.SocketException e)
        {
            var cfg = Config.Load();
            string clusterAdress = cfg.Cluster;
            MessageBox.Show(
                $"Failed to connect to the cluster at: '{clusterAdress}'.\n" +
                $"Please check your network connection.",
                "Connection Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        catch (InvalidOperationException e)
        {
            MessageBox.Show(
                $"An error occurred while refreshing job status.\n" +
                $"Error: {e.Message}",
                "Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
        finally
        {
            _jobManager.Disconnect();
        }
        RefreshStatus();
    }

    public void RefreshStatus()
    {
    try
    {
        var calcStatuses = new List<CalcStatus>();

        var unparsedQstat = _jobManager.QStat();
        
        var parsedQstat = QstatParser.ParseQstat(unparsedQstat);
        
        var calculationsOnCluster = _jobFinder.GetCalculationsOnCluster();
        
        foreach (var calculation in calculationsOnCluster)
        {
            if (calculation.MetaData?.JobId != null && parsedQstat.TryGetValue(calculation.MetaData.JobId, out var qstatState))
            {
                calculation.RefreshStatus(qstatState);
            }
            else
            {
                calculation.RefreshStatus(null);
            }
        }

        // Build status objects from calculations
        foreach (var calculation in calculationsOnCluster)
        {
            var calcStatus = CalcStatus.Create(calculation);
            calcStatuses.Add(calcStatus);
        }

        JobsOnCluster.Clear();
        foreach (var status in calcStatuses)
        {
            JobsOnCluster.Add(status);
        }
    }
    catch (Renci.SshNet.Common.SshAuthenticationException e)
    {
        var cfg = Config.Load();
        string userName = cfg.ClusterUsername;
        string keyPath = cfg.SshKeyPath;
        MessageBox.Show(
            $"Authentication with username '{userName}' and key '{keyPath}' failed.\n" +
            $"Please check your credentials.",
            "Authentication Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
    catch (System.Net.Sockets.SocketException e)
    {
        var cfg = Config.Load();
        string clusterAdress = cfg.Cluster;
        MessageBox.Show(
            $"Failed to connect to the cluster at: '{clusterAdress}'.\n" +
            $"Please check your network connection.",
            "Connection Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
    catch (InvalidOperationException e)
    {
        MessageBox.Show(
            $"An error occurred while refreshing job status.\n" +
            $"Error: {e.Message}",
            "Error",
            MessageBoxButton.OK,
            MessageBoxImage.Error
        );
    }
    finally
    {
        _jobManager.Disconnect();
    }
}

    private bool CanDownload()
    {
        return SelectedCalcstatus?.Calculation?.Paths?.RelativeDirectory != null &&
               !_fileHandler.DownloadManager.IsDownloading;
    }

    private async void DownloadSelection()
    {
        if (SelectedCalcstatus?.Calculation?.Paths?.RelativeDirectory == null)
            return;

        try
        {
            bool success = await _fileHandler.Download(SelectedCalcstatus.Calculation.Paths.RelativeDirectory);
            if (success)
            {
                MessageBox.Show(
                    "Job downloaded successfully. The files have been removed from the cluster",
                    "Download Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );
                RefreshStatus();
            }
        }
        catch (Exception e)
        {
            MessageBox.Show(
                $"An error occurred while downloading calculation files.\n" +
                $"Error: {e.Message}",
                "Download Error",
                MessageBoxButton.OK,
                MessageBoxImage.Error
            );
        }
    }
}
