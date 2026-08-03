using System.IO;
using System.Threading.Tasks.Dataflow;
using GaussianTool.Model.Configs;
using GaussianTool.Model.Enums;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model.FileHandling;

public class PathObject
{
    public string WindowsPath { get; set; }
    public string ClusterPath { get; set; }
    private bool IsClusterPath { get; set; }
    public PathType DestinationType { get; set; }
    private PathObject[]? _folderContent;

    public PathObject[]? FolderContent
    {
        get => _folderContent ??= GetFolderContent();
    }
    public string WindowsFolder { get; set; }
    public string ClusterFolder {get; set;}
    public string? Filename { get; set; }
    private ulong? _size;

    public ulong? Size
    {
        get => _size ??= GetSize();
    }
    
    public PathObject(string pathName, bool isClusterPath=false)
    {
        IsClusterPath = isClusterPath;
        if (isClusterPath)
        {
            ClusterPath = pathName;
            WindowsPath = ToWindows(pathName);
        }
        else
        {
            WindowsPath = pathName;
            ClusterPath = ToCluster(pathName);
        }
        
        DestinationType = GetDestinationType();
        
        WindowsFolder = GetFoldername(false);
        ClusterFolder = GetFoldername(true);
        if (DestinationType == PathType.File)
            Filename = GetFilename();
    }
    
    private PathType GetDestinationType()
    {
        string pathToCheck;
        
        if (!IsClusterPath)
        {
            pathToCheck = WindowsPath;
            if (Directory.Exists(pathToCheck))
            {
                return PathType.Directory;
            }
            if (File.Exists(pathToCheck))
            {
                return PathType.File;
            }
            return PathType.NotDefined;
        }
        
        pathToCheck = ClusterPath;
        var sshService = new SshService();
        string typeChar;
        try
        {
            sshService.Connect();
            typeChar = sshService.GetClusterDestinationType(pathToCheck);
            sshService.Disconnect();
        }
        catch (Exception e)
        {
            sshService.Disconnect();
            throw new InvalidOperationException("Failed to retrieve destination type.", e);
        }
        
        if (typeChar == "D")
        {
            return PathType.Directory;
        }
        if (typeChar == "F")
        {
            return PathType.File;
        }
        return PathType.NotDefined;
        
    }

    public PathObject Join(string otherPath)
    {
        if (IsClusterPath)
        {
            return new PathObject(ClusterPath + "/" + otherPath, isClusterPath: true);
        }
        return new PathObject(WindowsPath + "\\" + otherPath, isClusterPath: false);
    }

    private string ToCluster(string windowsPath)
    {
        var cfg = Config.Load();
        
        var elements = windowsPath.Split('\\');
        var rechnungenIndex = elements.IndexOf("Rechnungen");
        var clusterElements = elements.Skip(rechnungenIndex+1).ToArray();
        
        var relativePath = string.Join("/", clusterElements);
        return $"{cfg.ClusterRechnungenPath}/{relativePath}";
    }
    private string ToWindows(string clusterPath)
    {
        var cfg = Config.Load();
        
        var elements = clusterPath.Split('/');
        var rechnungenIndex = elements.IndexOf("Rechnungen");
        var clusterElements = elements.Skip(rechnungenIndex+1).ToArray();
        
        var relativePath = string.Join('\\', clusterElements);
        return $"{cfg.LocalRechnungenPath}\\{relativePath}";
    }

    private string GetFoldername(bool cluster)
    {
        string[] elements;
        
        if (cluster)
        {
            if (DestinationType == PathType.Directory)
                return ClusterPath;
            
            elements = ClusterPath.Split('/');
            return string.Join('/', elements.Take(elements.Length - 1));
        }
        
        if (DestinationType == PathType.Directory)
        {
            return WindowsPath;
        }
        elements = WindowsPath.Split('\\');
        return string.Join('\\', elements.Take(elements.Length - 1));
    }

    private string GetFilename()
    {
        string[] elements;
        
        if (IsClusterPath)
        {
            if (DestinationType == PathType.File)
            {
                elements = ClusterPath.Split('/');
                return elements[^1];
            }
        }
        else
        {
            if (DestinationType == PathType.File)
            {
                elements = WindowsPath.Split('\\');
                return elements[^1];
            }
        }
        return string.Empty;
    }

    public PathObject[]? GetFolderContent()
    {
        if (DestinationType != PathType.Directory)
            return null;

        PathObject[] pathsInDirectory;
        
        if (IsClusterPath)
        {
            try
            {
                pathsInDirectory = JobManager.Dir(this);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw new InvalidOperationException("Failed to retrieve folder content.", ex);
            }
            return pathsInDirectory;
        }
        string[] namesInDirectory = Directory.GetFileSystemEntries(WindowsPath);
        pathsInDirectory = namesInDirectory.Select(name => new PathObject(name, isClusterPath: false)).ToArray();
        return pathsInDirectory;
    }
    
    private ulong GetSize()
    {
        if (DestinationType == PathType.File)
        {
            if (IsClusterPath)
            {
                var size = JobManager.FileSize(this);
                return size;
            }
            var fileInfo = new FileInfo(WindowsPath);
            return (ulong)fileInfo.Length;
        }
        if (FolderContent == null)
            return 0;
        
        ulong totalSize = 0;
        foreach (var path in FolderContent)
        {
            if (path.Size.HasValue)
            {
                totalSize += path.Size.Value;
            }
        }
        return totalSize;
    }
    
    public PathObject? GetFileWithEnding(string ending)
    {
        return FolderContent?.FirstOrDefault(f => f.Filename?.EndsWith(ending) == true);
    }
}