using System.IO;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DirectoryExtension : PathExtension
{
    public DirectoryExtension(string relativePath, SshService sshService) : base(relativePath, sshService)
    {
        
    }
    
    public PathExtension[] GetContent(PathType pathType = PathType.Local)
    {
        if (pathType != PathType.Cluster && pathType != PathType.Local)
        {
            throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type");
        }

        var fullPath = GetPath(pathType);

        if (pathType == PathType.Local)
        {
            var entries=  Directory.GetFileSystemEntries(fullPath);
            PathExtension[] paths = entries.Select(entry =>
            {
                var relativePath = GetRelativePath(entry, PathType.Cluster);
                
                if (Directory.Exists(entry))
                    return (PathExtension)new DirectoryExtension(relativePath, SshService);

                return new FileExtension(relativePath, SshService);
            }).ToArray();
            return paths;
        }
        
        string[] clusterEntries = SshService.ConnectAndExecute(() => SshService.GetClusterContent(fullPath));
        
        PathExtension[] clusterPaths = clusterEntries.Select(entry =>
        {
            if (entry.EndsWith('/'))
                return (PathExtension)new DirectoryExtension(entry, SshService);

            return new FileExtension(entry, SshService);
        }).ToArray();
        
        return clusterPaths;
    }
    
    public long GetDirectorySize(PathType pathType = PathType.Local)
    {
        if (pathType != PathType.Cluster && pathType != PathType.Local)
        {
            throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type");
        }

        var fullPath = GetPath(pathType);
        long? size;

        if (pathType == PathType.Local)
        {
            var directoryInfo = new DirectoryInfo(fullPath);
            size = directoryInfo.EnumerateFiles(
                "*",
                SearchOption.AllDirectories).Sum(file => file.Length);
        }
        else
        {
            size = SshService.ConnectAndExecute(() => SshService.GetClusterDirectorySize(fullPath));
        }
        
        return size ?? throw new InvalidOperationException("Failed to retrieve directory size");
    }
}