using System.Collections.ObjectModel;
using System.Windows;
using GaussianTool.Model;
using GaussianTool.Model.Configs;
using GaussianTool.Model.FileHandling;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.ViewModel;

public class StatusBarViewModel : ViewModelBase
{
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
    
    public RelayCommand RefreshCommand => new RelayCommand(execute => Refresh());
    public RelayCommand DownloadCommand => new RelayCommand(
        execute => DownloadSelection(), canExecute => IsSelected());

    // public StatusBarViewModel()
    // {
    //     Refresh();
    // }

    
    
    public void Refresh()
    {
        try
        {
            JobManager.Connect();
            PathObject[] jobPaths = JobManager.GetJobsOnCluster();
            List<CalcStatus> calcStatuses = new List<CalcStatus>();
            foreach (var jobPath in jobPaths)
            {
                calcStatuses.Add(CalcStatus.Create(jobPath));
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
            JobManager.Disconnect();
        }
    }

    private bool IsSelected()
    {
        return SelectedCalcstatus != null;
    }

    private void DownloadSelection()
    {
        if (IsSelected())
        {
            Console.WriteLine($"Downloading selection: {SelectedCalcstatus.JobName}");
        }
        
        
        
        // try
        // {
        //
        // }
        // catch (Exception e)
        // {
        //     Console.WriteLine(e);
        //     throw;
        // }
        
        JobManager.DownloadFolder(SelectedCalcstatus.JobPath);
    }
}