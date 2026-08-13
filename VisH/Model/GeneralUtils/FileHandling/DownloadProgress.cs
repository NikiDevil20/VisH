namespace VisH.Model.GeneralUtils.FileHandling;

public record DownloadProgress(
    string CurrentFileName,
    long CurrentFileSize,
    long CurrentBytesDownloaded,
    long TotalBytes,
    long TotalBytesDownloaded);
