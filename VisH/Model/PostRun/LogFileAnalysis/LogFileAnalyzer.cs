using System.IO;
using VisH.Model;
using VisH.Model.FileHandling;
using VisH.Model.PostRun;


public class LogFileAnalyzer
{
    private readonly PythonBridge _pythonBridge;
    
    public LogFileAnalyzer(PythonBridge pythonBridge)
    {
        _pythonBridge = pythonBridge;
    }

    public CalcResults Run(PathObject calculationDirectory)
    {
        WriteJsonOutput(calculationDirectory);
        var calcResults = CalcResults.LoadFromJson(calculationDirectory);
        return calcResults;
    }
    
    private void WriteJsonOutput(PathObject calculationDirectory)
    {
        if (calculationDirectory.TryGetFileWithEnding("result.json") is not null)
            return;
        
        const string scriptName = "ParseLogfile.py";
        
        var logfile = calculationDirectory.GetFileWithEnding(".log").WindowsPath;
        var directoryPath = calculationDirectory.WindowsPath;
        
        string[] scriptArgs = [logfile, directoryPath] ;
        
        var error = _pythonBridge.ExecuteScript(scriptName, scriptArgs);
        Console.WriteLine(error);
    }
    
    
}
