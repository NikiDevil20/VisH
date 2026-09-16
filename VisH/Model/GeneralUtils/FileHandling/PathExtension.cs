using System.Text.RegularExpressions;
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
        ClusterBaseDir = SanitizeClusterBaseDir(config.ClusterRechnungenPath, config.ClusterUsername);
        RelativePath = pathComponents.Where(component => !string.IsNullOrEmpty(component)).ToArray();
        SshService = sshService;
    }

    private static string SanitizeClusterBaseDir(string clusterBaseDir, string username)
    {
        if (string.IsNullOrWhiteSpace(clusterBaseDir)) return string.Empty;
        var normalized = clusterBaseDir.Replace('\\', '/');

        if (!string.IsNullOrWhiteSpace(username) && normalized.StartsWith($"/home/{username}/", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized[($"/home/{username}/".Length)..];
        }
        else if (!string.IsNullOrWhiteSpace(username) && normalized.Equals($"/home/{username}", StringComparison.OrdinalIgnoreCase))
        {
            normalized = string.Empty;
        }
        else if (Regex.IsMatch(normalized, @"^/home/[^/]+(/|$)"))
        {
            normalized = Regex.Replace(normalized, @"^/home/[^/]+/?", "");
        }

        return normalized.TrimStart('/');
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
        var normalizedFullPath = fullPath.Replace('\\', '/').TrimStart('/');
        var normalizedBaseDir = baseDir.Replace('\\', '/').Trim('/');

        if (pathType == PathType.Cluster)
        {
            if (Regex.IsMatch(normalizedFullPath, @"^home/[^/]+(/|$)"))
            {
                normalizedFullPath = Regex.Replace(normalizedFullPath, @"^home/[^/]+/?", "");
            }
        }

        if (!string.IsNullOrEmpty(normalizedBaseDir) && normalizedFullPath.StartsWith(normalizedBaseDir + '/', StringComparison.OrdinalIgnoreCase))
        {
            normalizedFullPath = normalizedFullPath[(normalizedBaseDir.Length + 1)..];
        }
        else if (!string.IsNullOrEmpty(normalizedBaseDir) && normalizedFullPath.Equals(normalizedBaseDir, StringComparison.OrdinalIgnoreCase))
        {
            normalizedFullPath = string.Empty;
        }

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
        var normalizedBase = baseDirectory.Replace('\\', separator).Replace('/', separator).Trim(separator);
        var relativePath = string.Join(separator, pathComponents);
        if (string.IsNullOrEmpty(normalizedBase))
        {
            return relativePath;
        }
        return string.IsNullOrEmpty(relativePath)
            ? normalizedBase
            : normalizedBase + separator + relativePath;
    }

    private static string[] SplitPath(string path) =>
        path.Split(['/', '\\'], StringSplitOptions.RemoveEmptyEntries);
}
