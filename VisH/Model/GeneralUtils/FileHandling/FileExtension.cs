using System.IO;
using VisH.Model.Enums;

namespace VisH.Model.GeneralUtils.FileHandling;

public class FileExtension : PathExtension
{
    public FileExtension(string relativePath)
    {
        RelativePath = relativePath;
    }

    public string GetFileName(PathType pathType = PathType.Local)
    {
        var fullPath = ToString(pathType);

        if (pathType == PathType.Local)
        {
            return Path.GetFileName(fullPath);
        }
        
        
    }
}