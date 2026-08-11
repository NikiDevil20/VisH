namespace VisH.Model.GeneralUtils.FileHandling;

public record DownloadProgress(
    string CurrentFileName,
    ulong CurrentFileSize,
    ulong CurrentBytesDownloaded,
    ulong TotalBytes,
    ulong TotalBytesDownloaded);
