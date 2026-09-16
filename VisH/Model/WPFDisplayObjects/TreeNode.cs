using System.Collections.ObjectModel;
using System.IO;

namespace VisH.Model.WPFDisplayObjects;

public class TreeNode
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; }
    
    public ObservableCollection<TreeNode> Children { get; set; } = [];

    public static TreeNode BuildTree(string path)
    {
        DirectoryInfo dir = new(path);

        TreeNode node = new()
        {
            Name = dir.Name,
            FullPath = dir.FullName,
            IsDirectory = true
        };

        foreach (var subDir in dir.GetDirectories())
        {
            node.Children.Add(BuildTree(subDir.FullName));
        }
        
        return node;
    }
    
    
    
    
}
