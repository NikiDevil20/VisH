using GaussianTool.Model.Enums;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model.FileHandling;
using System.IO;

public static class FileHandler
{
    public static string? DownloadDirectory(PathObject path)
    {
        if (path.DestinationType != PathType.Directory)
        {
            return "PointsToFile";
        }

        JobManager.DownloadFolder(path);
        return null;
    }

}