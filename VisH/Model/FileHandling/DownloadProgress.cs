namespace VisH.Model.FileHandling;

public record DownloadProgress(
    string CurrentFileName,
    ulong CurrentFileSize,
    ulong CurrentBytesDownloaded,
    ulong TotalBytes,
    ulong TotalBytesDownloaded);
