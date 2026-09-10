using System.ComponentModel;
using System.Windows.Input;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.ViewModel;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DownloadManager : ViewModelBase
{
    private readonly JobManager _jobManager;
    private readonly SemaphoreSlim _downloadLock = new(1, 1);

    public DownloadManager(JobManager jobManager)
    {
        _jobManager = jobManager;
    }

    private bool _isDownloading { get; set; }
    public bool IsDownloading
    {
        get => _isDownloading;
        set
        {
            if (_isDownloading != value)
            {
                _isDownloading = value;
                OnPropertyChanged();
                CommandManager.InvalidateRequerySuggested();
            }
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
    
    public async Task<bool> DownloadFolder(DirectoryExtension directory)
    {
        if (IsDownloading)
        {
            return false;
        }

        await _downloadLock.WaitAsync();
        try
        {
            IsDownloading = true;
            DownloadPercentage = 0;
            CurrentFileName = "";
            LocalDownloadPercentage = 0;
            
            var progress = new Progress<DownloadProgress>(p =>
            {
                CurrentFileName = $"Downloading {p.CurrentFileName}";
                DownloadPercentage = p.TotalBytes > 0 ? 100.0 * p.TotalBytesDownloaded / p.TotalBytes : 0;
                LocalDownloadPercentage = p.CurrentFileSize > 0 ? 100.0 * p.CurrentBytesDownloaded / p.CurrentFileSize : 0;
            });

            return await Task.Run(() => _jobManager.DownloadFolder(directory, progress));
        }
        finally
        {
            DownloadPercentage = 100;
            CurrentFileName = "";
            LocalDownloadPercentage = 100;
            IsDownloading = false;
            _downloadLock.Release();
        }
    }
}
