using System.Configuration;
using System.Data;
using System.Windows;
using Serilog;
using VisH.Model.GeneralUtils;

namespace VisH;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{ 
    protected override void OnStartup(StartupEventArgs e)
    {
        var config = Config.Load();
        var logPath = config.LocalRechnungenPath + "\\log-.txt";
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();
        
        base.OnStartup(e);
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
