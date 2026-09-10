using System.IO;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DirectoryExtension : PathExtension
{
    public DirectoryExtension(string relativePath, SshService sshService) : base(relativePath, sshService) { }

    public DirectoryExtension(IEnumerable<string> pathComponents, SshService sshService)
        : base(pathComponents, sshService) { }

    public PathExtension[] GetContent(PathType pathType = PathType.Local)
    {
        var fullPath = GetPath(pathType);
        if (pathType == PathType.Local)
        {
            return Directory.GetFileSystemEntries(fullPath).Select(entry =>
            {
                var relativePath = GetRelativePath(entry, PathType.Local);
                return Directory.Exists(entry)
                    ? (PathExtension)new DirectoryExtension(relativePath, SshService)
                    : new FileExtension(relativePath, SshService);
            }).ToArray();
        }

        if (pathType != PathType.Cluster)
            throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type");

        return SshService.ConnectAndExecute(() => SshService.GetClusterContent(fullPath)).Select(entry =>
        {
            var relativePath = GetRelativePath(entry, PathType.Cluster);
            return entry.EndsWith('/')
                ? (PathExtension)new DirectoryExtension(relativePath, SshService)
                : new FileExtension(relativePath, SshService);
        }).ToArray();
    }

    public long GetDirectorySize(PathType pathType = PathType.Local)
    {
        var fullPath = GetPath(pathType);
        long? size = pathType switch
        {
            PathType.Local => new DirectoryInfo(fullPath).EnumerateFiles("*", SearchOption.AllDirectories).Sum(file => file.Length),
            PathType.Cluster => SshService.ConnectAndExecute(() => SshService.GetClusterDirectorySize(fullPath)),
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type")
        };
        return size ?? throw new InvalidOperationException("Failed to retrieve directory size");
    }
}
