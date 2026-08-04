using System.IO;

namespace VisH.Model.Logging;

public class Logger
{
    private static string _logfilePath =
        "C:\\Users\\nikla\\OneDrive - Heinrich-Heine-Universitat Dusseldorf\\Dokumente\\Rechnungen\\log.txt";

    public static void Info(string message)
    {
        Console.WriteLine(message);
        File.AppendAllText(_logfilePath, message);
    }
}
