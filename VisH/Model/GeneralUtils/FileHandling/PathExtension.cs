using VisH.Model.Enums;
using VisH.Model.GeneralUtils.Hilbert;

namespace VisH.Model.GeneralUtils.FileHandling;

public class PathExtension
{
    public string LocalBaseDir { get; init; }
    public string ClusterBaseDir { get; init; }
    public IReadOnlyList<string> RelativePath { get; }
    public SshService SshService { get; init; }

    public PathExtension(string relativePath, SshService sshService)
        : this(SplitPath(relativePath), sshService) { }

    public PathExtension(IEnumerable<string> pathComponents, SshService sshService)
    {
        var config = Config.Load();
        LocalBaseDir = config.LocalRechnungenPath;
        ClusterBaseDir = config.ClusterRechnungenPath;
        RelativePath = pathComponents.Where(component => !string.IsNullOrEmpty(component)).ToArray();
        SshService = sshService;
    }

    public string GetPath(PathType pathType = PathType.Local)
    {
        return pathType switch
        {
            PathType.Local => CombinePath(RelativePath, LocalBaseDir, '\\'),
            PathType.Cluster => CombinePath(RelativePath, ClusterBaseDir, '/'),
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
        };
    }

    public string GetFileName(PathType pathType = PathType.Local)
    {
        _ = GetPath(pathType);
        return RelativePath.LastOrDefault() ?? string.Empty;
    }

    public IReadOnlyList<string> GetRelativePath(string fullPath, PathType pathType = PathType.Local)
    {
        var baseDir = pathType switch
        {
            PathType.Local => LocalBaseDir,
            PathType.Cluster => ClusterBaseDir,
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
        };
        var normalizedFullPath = fullPath.Replace('\\', '/');
        var normalizedBaseDir = baseDir.Replace('\\', '/').TrimEnd('/');

        if (normalizedFullPath.StartsWith(normalizedBaseDir + '/', StringComparison.Ordinal))
            normalizedFullPath = normalizedFullPath[(normalizedBaseDir.Length + 1)..];

        return SplitPath(normalizedFullPath);
    }

    protected string CombinePath(IEnumerable<string> pathComponents, PathType pathType)
    {
        return pathType switch
        {
            PathType.Local => CombinePath(pathComponents, LocalBaseDir, '\\'),
            PathType.Cluster => CombinePath(pathComponents, ClusterBaseDir, '/'),
            _ => throw new ArgumentOutOfRangeException(nameof(pathType), pathType, null)
        };
    }

    private static string CombinePath(IEnumerable<string> pathComponents, string baseDirectory, char separator)
    {
        var normalizedBase = baseDirectory.Replace('\\', separator).Replace('/', separator);
        var relativePath = string.Join(separator, pathComponents);
        return string.IsNullOrEmpty(relativePath)
            ? normalizedBase
            : normalizedBase.TrimEnd(separator) + separator + relativePath;
    }

    private static string[] SplitPath(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
