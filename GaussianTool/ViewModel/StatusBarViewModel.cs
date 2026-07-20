using System.Collections.ObjectModel;
using GaussianTool.Model;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.ViewModel;

public class StatusBarViewModel : ViewModelBase
{
    public ObservableCollection<CalcStatus> JobsOnCluster { get; } = new();
    public RelayCommand RefreshCommand => new RelayCommand(execute => Refresh());

    public StatusBarViewModel()
    {
        
    }

    private async void Refresh()
    {
        // string[] jobIds = ClusterOverview.GetJobsOnCluster();

        string[] jobPaths = JobManager.GetJobsOnCluster();
        
        List<CalcStatus> calcStatuses = new List<CalcStatus>();
        var tasks = jobPaths.Select(async jobPath => await CalcStatus.CreateAsync(jobPath));
        calcStatuses = (await Task.WhenAll(tasks)).ToList();
        
        JobsOnCluster.Clear();
        foreach (var status in calcStatuses)
        {
            JobsOnCluster.Add(status);
        }
    }
}