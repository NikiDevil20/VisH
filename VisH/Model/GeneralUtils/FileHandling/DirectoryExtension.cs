using System.IO;
using VisH.Model.Enums;

namespace VisH.Model.GeneralUtils.FileHandling;

public class DirectoryExtension : PathExtension
{
    public DirectoryExtension(string relativePath)
    {
        RelativePath = relativePath;
    }
    
}