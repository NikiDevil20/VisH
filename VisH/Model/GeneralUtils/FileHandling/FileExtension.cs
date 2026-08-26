using System.IO;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class FileExtension : PathExtension
{
    public FileExtension(string relativePath, SshService sshService) : base(relativePath, sshService) { }

    public FileExtension(IEnumerable<string> pathComponents, SshService sshService)
        : base(pathComponents, sshService) { }

    public string? GetDirectory(PathType pathType = PathType.Local)
    {
        return CombinePath(RelativePath.Take(Math.Max(0, RelativePath.Count - 1)), pathType);
    }

    public long GetFileSize(PathType pathType = PathType.Local)
    {
        var fullPath = GetPath(pathType);
        long? size = pathType switch
        {
            PathType.Local => new FileInfo(fullPath).Length,
            PathType.Cluster => SshService.ConnectAndExecute(() => SshService.GetClusterFileSize(fullPath)),
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, "Invalid path type")
        };
        return size ?? throw new InvalidOperationException("Failed to retrieve file size");
    }
}
