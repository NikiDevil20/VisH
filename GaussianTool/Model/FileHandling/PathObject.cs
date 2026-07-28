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
    public PathObject[]? FolderContent { get; set; }
    public string Foldername { get; set; }
    public string? Filename { get; set; }
    
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
        if (DestinationType == PathType.NotDefined)
        {
            throw new InvalidOperationException("Path not found.");
        }
        FolderContent = GetFolderContent();
        Foldername = GetFoldername();
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

    private string GetFoldername()
    {
        string[] elements;
        
        if (IsClusterPath)
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

    private PathObject[]? GetFolderContent()
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
}