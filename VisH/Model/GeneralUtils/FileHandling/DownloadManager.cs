using System.ComponentModel;
using System.IO;
using System.Windows.Input;
using VisH.Model.CalculationObject;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.Model.GeneralUtils;
using VisH.ViewModel;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DownloadManager : ViewModelBase
{
    private readonly JobManager _jobManager;
    private readonly PythonBridge _pythonBridge = new();
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

            var success = await Task.Run(() => _jobManager.DownloadFolder(directory, progress));
            if (success)
            {
                UpdateDownloadedCalculation(directory);
            }

            return success;
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

    private void UpdateDownloadedCalculation(DirectoryExtension directory)
    {
        var jsonPath = directory.GetContent(PathType.Local)
            .OfType<FileExtension>()
            .FirstOrDefault(f => f.GetFileName(PathType.Local).Equals("calculation.json", StringComparison.OrdinalIgnoreCase));

        var logPath = directory.GetContent(PathType.Local)
            .OfType<FileExtension>()
            .FirstOrDefault(f => f.GetFileName(PathType.Local).EndsWith(".log", StringComparison.OrdinalIgnoreCase));

        var lgPath = directory.GetContent(PathType.Local)
            .OfType<FileExtension>()
            .FirstOrDefault(f => f.GetFileName(PathType.Local).EndsWith(".lg", StringComparison.OrdinalIgnoreCase));

        if (jsonPath is null || logPath is null || lgPath is null)
        {
            return;
        }

        var localDirectory = Path.GetDirectoryName(jsonPath.GetPath(PathType.Local));
        if (string.IsNullOrWhiteSpace(localDirectory))
        {
            return;
        }

        var scriptResult = _pythonBridge.ExecuteScriptWithStatus("ParseLogfile.py", [logPath.GetPath(PathType.Local), localDirectory]);
        if (!string.IsNullOrWhiteSpace(scriptResult.stdout) || !string.IsNullOrWhiteSpace(scriptResult.stderr))
        {
            Console.WriteLine(scriptResult.stdout);
            Console.WriteLine(scriptResult.stderr);
        }

        var resultJsonPath = Path.Combine(localDirectory, "result.json");
        if (!File.Exists(resultJsonPath))
        {
            Console.WriteLine($"Expected result file not found: {resultJsonPath}");
            return;
        }

        if (!File.Exists(jsonPath.GetPath(PathType.Local)))
        {
            return;
        }

        var calculation = Calculation.FromJson(jsonPath.GetPath(PathType.Local), null!);
        calculation.UpdateFromDownloadedFiles(
            resultJsonPath,
            lgPath.GetPath(PathType.Local));
        calculation.SaveCalculation();
    }
}
