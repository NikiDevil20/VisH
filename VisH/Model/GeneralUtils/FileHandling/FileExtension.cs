using System.IO;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class FileExtension : PathExtension
{
    public FileExtension(string relativePath, SshService sshService) : base(relativePath, sshService)
    {
        
    }

    public string? GetDirectory(PathType pathType = PathType.Local)
    {
        if (pathType != PathType.Cluster && pathType != PathType.Local)
        {
            throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type");
        }

        var fullPath = GetPath(pathType);

        if (pathType == PathType.Local)
        {
            return Path.GetDirectoryName(fullPath);
        }

        var splitPath = fullPath.Split('/');
        return string.Join('/', splitPath.Take(splitPath.Length - 1));
    }
    
    public long GetFileSize(PathType pathType = PathType.Local)
    {
        if (pathType != PathType.Cluster && pathType != PathType.Local)
        {
            throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type");
        }

        var fullPath = GetPath(pathType);
        long? size;

        if (pathType == PathType.Local)
        {
            var fileInfo = new FileInfo(fullPath);
            size = fileInfo.Length;
        }
        else
        {
            size = SshService.ConnectAndExecute(() => SshService.GetClusterFileSize(fullPath));
        }

        return size ?? throw new InvalidOperationException("Failed to retrieve file size");
    }
}