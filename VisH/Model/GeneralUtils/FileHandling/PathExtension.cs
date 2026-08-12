using System.IO;
using VisH.Model.Enums;

namespace VisH.Model.GeneralUtils.FileHandling;

public class PathExtension
{
    public string LocalBaseDir { get; init; }
    public string ClusterBaseDir { get; init; }
    public string RelativePath { get; init; }

    public PathExtension()
    {
        var config = Config.Load();
        LocalBaseDir = config.LocalRechnungenPath;
        ClusterBaseDir = config.ClusterRechnungenPath;
        RelativePath = string.Empty;
    }
    
    public string ToString(PathType pathType=PathType.Local)
    {
        switch (pathType)
        {
            case PathType.Local:
                return Path.Combine(LocalBaseDir, RelativePath);
            case PathType.Cluster:
                var clusterPath = Path.Combine(ClusterBaseDir, RelativePath).Replace('\\', '/');
                return clusterPath;
        }

        throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null);
    }
}