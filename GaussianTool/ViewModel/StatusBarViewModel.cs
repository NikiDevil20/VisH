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

    private void Refresh()
    {
        string[] jobIds = ClusterOverview.GetJobsOnCluster();
        
        List<CalcStatus> calcStatuses = new List<CalcStatus>();
        foreach (var jobId in jobIds)
        {
            calcStatuses.Add(new CalcStatus(jobId));
        }
        JobsOnCluster.Clear();
        foreach (var status in calcStatuses)
        {
            JobsOnCluster.Add(status);
        }
    }
}