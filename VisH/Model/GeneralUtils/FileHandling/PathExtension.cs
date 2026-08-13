using System.IO;
using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class PathExtension
{
    public string LocalBaseDir { get; init; }
    public string ClusterBaseDir { get; init; }
    public string RelativePath { get; init; }
    public SshService SshService { get; init; }

    public PathExtension(string relativePath, SshService sshService)
    {
        var config = Config.Load();
        LocalBaseDir = config.LocalRechnungenPath;
        ClusterBaseDir = config.ClusterRechnungenPath;
        RelativePath = relativePath;
        SshService = sshService;
    }
    
    public string GetPath(PathType pathType=PathType.Local)
    {
        switch (pathType)
        {
            case PathType.Local:
            {
                var fullPath = Path.Combine(LocalBaseDir, RelativePath);
                return fullPath;
            }
            case PathType.Cluster:
            {
                var clusterPath = Path.Combine(ClusterBaseDir, RelativePath).Replace('\\', '/');
                Console.WriteLine("Path");
                Console.WriteLine(clusterPath);
                return clusterPath;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
    }
    
    public string GetFileName(PathType pathType=PathType.Local)
    {
        var path = GetPath(pathType);
        return pathType == PathType.Local
            ? Path.GetFileName(path)
            : path.Split('/')[^1];
    }

    public string GetRelativePath(string fullPath, PathType pathType = PathType.Local)
    {
        string baseDir = pathType switch
        {
            PathType.Local => LocalBaseDir,
            PathType.Cluster => ClusterBaseDir,
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
        };

        string normalizedFullPath = fullPath.Replace('\\', '/');
        string normalizedBaseDir = baseDir.Replace('\\', '/').TrimEnd('/');

        if (normalizedFullPath.StartsWith(normalizedBaseDir + '/', StringComparison.Ordinal))
        {
            return normalizedFullPath[(normalizedBaseDir.Length + 1)..];
        }

        return normalizedFullPath;
    }
}