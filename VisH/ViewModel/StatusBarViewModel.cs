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
        var selectedCalculation = SelectedCalcstatus?.Calculation;
        if (selectedCalculation?.Paths?.RelativeDirectory == null)
            return;

        try
        {
            var downloadDirectory = selectedCalculation.Paths.RelativeDirectory;
            if (IsEddCalculation(selectedCalculation))
            {
                var eddPair = FindEddPair(selectedCalculation);
                if (eddPair == null)
                {
                    MessageBox.Show(
                        "Die zugehörige TD- oder SP-Rechnung ist noch nicht erfolgreich abgeschlossen. " +
                        "Der EDD-Ordner wurde nicht heruntergeladen.",
                        "EDD noch nicht vollständig",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                    return;
                }

                downloadDirectory = GetEddSharedDirectory(selectedCalculation);
            }

            bool success = await _fileHandler.Download(downloadDirectory);
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

    private bool IsEddCalculation(Calculation calculation)
    {
        return calculation.GaussianParameters?.IsEddDensityCalculation == true &&
               calculation.Paths.RelativeDirectory.RelativePath.LastOrDefault() is "td" or "sp";
    }

    private CalcStatus? FindEddPair(Calculation calculation)
    {
        var sharedPath = calculation.Paths!.RelativeDirectory.RelativePath
            .Take(calculation.Paths.RelativeDirectory.RelativePath.Count - 1)
            .ToArray();

        return JobsOnCluster
            .FirstOrDefault(status =>
                status.Calculation?.Paths?.RelativeDirectory.RelativePath.SequenceEqual(
                    sharedPath.Append(
                        calculation.Paths.RelativeDirectory.RelativePath.Last() == "td" ? "sp" : "td")) == true &&
                status.Calculation.MetaData?.JobState == JobState.Successful);
    }

    private DirectoryExtension GetEddSharedDirectory(Calculation calculation)
    {
        var sharedPath = calculation.Paths!.RelativeDirectory.RelativePath
            .Take(calculation.Paths.RelativeDirectory.RelativePath.Count - 1);
        return new DirectoryExtension(sharedPath, _sshService);
    }
}
