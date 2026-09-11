using System.Diagnostics;
using System.IO;
using VisH.Model.GeneralUtils;

namespace VisH.Model.GeneralUtils;

public class PythonBridge
{
    private string pythonFolderPath { get; set; }
    private string pythonExePath { get; set; }
    
    public PythonBridge()
    {
        // string baseDir = AppContext.BaseDirectory;
        string baseDir = @"C:\Users\nikla\RiderProjects\VisH\VisH";
        pythonFolderPath = Path.Combine(baseDir, "PythonScripts");
        pythonExePath = Path.Combine(pythonFolderPath, "venv",  "python.exe");
        
    }

    public string ExecuteScript(string scriptName, string[] scriptArgs)
    {
        return ExecuteScriptWithStatus(scriptName, scriptArgs).stdout;
    }

    public (int exitCode, string stdout, string stderr) ExecuteScriptWithStatus(string scriptName, string[] scriptArgs)
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
        if (process == null)
        {
            return (-1, string.Empty, "Failed to start python process.");
        }

        string output = process.StandardOutput.ReadToEnd();
        string? error =  process.StandardError.ReadToEnd();
        
        process.WaitForExit();

        return (process.ExitCode, output, error ?? string.Empty);
    }
    
}
