using System.Collections.ObjectModel;
using System.Windows;
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
            }
        }
    }
    
    public RelayCommand RefreshCommand => new RelayCommand(execute => RefreshStatus());
    
    public RelayCommand DownloadCommand => new RelayCommand(
        execute => DownloadSelection(), canExecute => IsSelected());
    
    private PathObject[] _pathsOnCluster { get; set; }
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
    // var jobFinder = new JobFinder(_sshService, _jobManager);
    var statusAddedCounter = 0;
    
    try
    {
        var calcStatuses = new List<CalcStatus>();

        var unparsedQstat = _jobManager.QStat();
        
        var parsedQstat = QstatParser.ParseQstat(unparsedQstat);
        
        var calculationsOnCluster = _jobFinder.GetCalculationsOnCluster();
        
        foreach (var runningJob in parsedQstat)
        {
            var jobId = runningJob.Key;
            var jobState = runningJob.Value;

            foreach (var calculation in calculationsOnCluster)
            {
                if (calculation.MetaData?.JobId == jobId)
                {
                    calculation.MetaData.JobState = jobState;
                    statusAddedCounter += 1;
                }
            }
        }

        if (statusAddedCounter != calculationsOnCluster.Length)
        {
            foreach (var calculationOnCluster in calculationsOnCluster)
            {
                calculationOnCluster.RefreshStatus();
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

    private bool IsSelected()
    {
        return SelectedCalcstatus != null;
    }

    private async void DownloadSelection()
    {
        // if (IsSelected())
        // {
        //     await _fileHandler.Download(SelectedCalcstatus.JobPath);
        // }
    }
}
