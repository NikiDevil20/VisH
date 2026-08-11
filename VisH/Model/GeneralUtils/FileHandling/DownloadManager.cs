using System.ComponentModel;
using VisH.Model.Hilbert;
using VisH.ViewModel;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DownloadManager : ViewModelBase
{
    private bool _isDownloading { get; set; }
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            _isDownloading = value;
            OnPropertyChanged();
        }
    }
    private double _downloadPercentage { get; set; }
    public double DownloadPercentage
    {
        get => _downloadPercentage;
        set
        {
            _downloadPercentage = value;
            OnPropertyChanged();
        }
    }
    private string _currentFileName { get; set; }
    public string CurrentFileName
    {
        get => _currentFileName;
        set
        {
            _currentFileName = value;
            OnPropertyChanged();
        }
    }
    private double _localDownloadPercentage { get; set; }
    public double LocalDownloadPercentage
    {
        get => _localDownloadPercentage;
        set
        {
            _localDownloadPercentage = value;
            OnPropertyChanged();
        }
    }
    
    public async Task DownloadFolder(PathObject path)
    {
        IsDownloading = true;
        DownloadPercentage = 0;
        CurrentFileName = "";
        
        var progress = new Progress<DownloadProgress>(p =>
        {
            CurrentFileName = $"Downloading {p.CurrentFileName}";
            DownloadPercentage = 100.0 * p.TotalBytesDownloaded / p.TotalBytes;
            LocalDownloadPercentage = 100.0 * p.CurrentBytesDownloaded / p.CurrentFileSize;
        });
        try
        {
            await Task.Run(() => { JobManager.DownloadFolder(path, progress); });
        }
        finally
        {
            DownloadPercentage = 100;
            CurrentFileName = "";
            LocalDownloadPercentage = 100;
            IsDownloading = false;
        }
    }
    
}
