using System.Diagnostics;
using System.IO;
using GaussianTool.Model.Configs;

namespace GaussianTool.Model;

public class PythonBridge
{
    private string pythonFolderPath { get; set; }
    private string pythonExePath { get; set; }
    
    public PythonBridge()
    {
        // string baseDir = AppContext.BaseDirectory;
        string baseDir = @"C:\\Users\\Niklas\\RiderProjects\\GaussianTool\\GaussianTool";
        pythonFolderPath = Path.Combine(baseDir, "PythonScripts");
        pythonExePath = Path.Combine(pythonFolderPath, "venv",  "python.exe");
        
    }

    public string ExecuteScript(string scriptName, string[] scriptArgs)
    {
        string scriptPath = Path.Combine(pythonFolderPath, scriptName);

        var psi = new ProcessStartInfo
        {
            FileName = pythonExePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };
        
        psi.ArgumentList.Add(scriptPath);
        foreach (string arg in scriptArgs)
        {
            psi.ArgumentList.Add(arg);
        }
        
        using Process process = Process.Start(psi);
        
        string output = process.StandardOutput.ReadToEnd();
        string? error =  process.StandardError.ReadToEnd();
        
        process.WaitForExit();
        
        if (!string.IsNullOrWhiteSpace(error))
            return error;
        return output;
    }
    
}