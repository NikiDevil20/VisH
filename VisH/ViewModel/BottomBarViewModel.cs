using System.ComponentModel;
using VisH.Model.FileHandling;

namespace VisH.ViewModel;

public class BottomBarViewModel : ViewModelBase
{
    private DownloadManager _downloadManager;

    public string CurrentFileName => _downloadManager.CurrentFileName;
    public string TotalPercentage => _downloadManager.DownloadPercentage.ToString();
    public string LocalPercentage => _downloadManager.LocalDownloadPercentage.ToString();

    public BottomBarViewModel(DownloadManager downloadManager)
    {
        _downloadManager = downloadManager;
        Console.WriteLine("BottomBarViewModel initialized.");
        _downloadManager.PropertyChanged += DownloadManagerOnPropertyChanged;
    }
    
    private void DownloadManagerOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        switch (e.PropertyName)
        {
            case nameof(DownloadManager.CurrentFileName):
                OnPropertyChanged(nameof(CurrentFileName));
                break;

            case nameof(DownloadManager.DownloadPercentage):
                OnPropertyChanged(nameof(TotalPercentage));
                break;

            case nameof(DownloadManager.LocalDownloadPercentage):
                OnPropertyChanged(nameof(LocalPercentage));
                break;
        }
    }
}
