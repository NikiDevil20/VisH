using System.IO;
using GaussianTool.Model.Hilbert;

namespace GaussianTool.Model.FileHandling;

public class PathObject
{
    public string? WindowsPath { get; set; }
    public string? ClusterPath { get; set; }
    public bool IsClusterPath { get; set; }


    public PathObject(string pathName, bool isClusterPath=false)
    {
        IsClusterPath = isClusterPath;
        if (isClusterPath)
        {
            ClusterPath = pathName;
        }
        else
        {
            WindowsPath = pathName;
        }
    }

    // private string ToCluster(string pathWindows)
    // {
    //     return pathWindows.Replace("\\", "/");
    // }
    
    public string GetDestinationType()
    {
        if (IsClusterPath)
        {
            string pathToCheck = WindowsPath;
            if (Directory.Exists(pathToCheck))
            {
                return "Directory";
            }
            if (File.Exists(pathToCheck))
            {
                return "File";
            }
            return "Unknown";
        }
        else
        {
            string pathToCheck = ClusterPath;
            var sshService = new SshService();
            string typeChar = sshService.GetClusterDestinationType(pathToCheck);
            
            if (typeChar == "D")
            {
                return "Directory";
            }
            if (typeChar == "F")
            {
                return "File";
            }
            return "Unknown";
        }
    }
}