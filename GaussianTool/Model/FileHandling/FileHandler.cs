namespace GaussianTool.Model.FileHandling;
using System.IO;

public class FileHandler
{
    public Calculation Calculation {get; set; }

    public FileHandler(Calculation calculation)
    {
        Calculation = calculation;
    }

    private void WriteFile(string path, string content)
    {
        File.WriteAllText(path, content);
    }

}