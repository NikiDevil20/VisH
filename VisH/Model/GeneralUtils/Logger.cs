using System.IO;

namespace VisH.Model.GeneralUtils;

public class Logger
{
    private static string LogfilePath =>
        Path.Combine(Config.UserDataDirectory, "log.txt");

    public static void Info(string message)
    {
        Console.WriteLine(message);
        try
        {
            Directory.CreateDirectory(Config.UserDataDirectory);
            File.AppendAllText(LogfilePath, message + Environment.NewLine);
        }
        catch
        {
            // Ignore logging failures
        }
    }
}
