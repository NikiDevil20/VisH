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
    
    
    public StatusBarViewModel(FileHandler fileHandler)
    {
        _fileHandler = fileHandler;
        _changeToClusterWasMade = true;
        _fileHandler.ClusterChanged += () => RefreshJobList();
    }

    public void RefreshJobList()
    {
        try
        {
            JobManager.Connect();
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
            JobManager.Disconnect();
        }
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        var jobFinder = new JobFinder();
        var statusAddedCounter = 0;
        
        try
        {
            var calcStatuses = new List<CalcStatus>();

            var unparsedQstat = JobManager.QStat();
            
            var parsedQstat = QstatParser.ParseQstat(unparsedQstat);
            
            var calculationsOnCluster = jobFinder.GetCalculationsOnCluster();

            foreach (var runningJob in parsedQstat)
            {
                var jobId = runningJob.Key;
                var jobState = runningJob.Value;

                foreach (var calculation in calculationsOnCluster)
                {
                    if (calculation.MetaData.JobId == jobId)
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

            foreach (var jobPath in _pathsOnCluster)
            // {
            //     calcStatuses.Add(CalcStatus.Create(jobPath));
            // }

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
            JobManager.Disconnect();
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
