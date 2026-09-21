using System.IO;
using System.Windows;
using Serilog;
using VisH.Model.GeneralUtils;
using VisH.Model.GeneralUtils.Hilbert;
using VisH.View.Windows.SettingsWindow;
using Renci.SshNet.Common;

namespace VisH;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{ 
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        Config? config = null;
        string validationError;
        try
        {
            config = Config.Load();
            if (config.ContainsPlaceholders(out string placeholderError))
            {
                OpenInvalidConfigurationWindow(placeholderError, isPlaceholderPrompt: true);
                return;
            }

            if (!SshService.TryValidateConfiguration(config, out validationError))
            {
                OpenInvalidConfigurationWindow(validationError);
                return;
            }
        }
        catch (Exception exception) when (IsSshRelated(exception))
        {
            OpenInvalidConfigurationWindow(exception.Message);
            return;
        }

        string appDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VisH");
        string logDirectory = Path.Combine(appDataDirectory, "logs");
        Directory.CreateDirectory(logDirectory);
        string logPath = Path.Combine(logDirectory, "log-.txt");
        
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                logPath,
                rollingInterval: RollingInterval.Day,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message:lj}{NewLine}{Exception}"
            )
            .CreateLogger();

        try
        {
            // Create the application-wide SSH service once and pass it through the object graph.
            new MainWindow(new SshService()).Show();
        }
        catch (Exception exception) when (IsSshRelated(exception))
        {
            OpenInvalidConfigurationWindow(exception.Message);
        }
    }

    private static bool IsSshRelated(Exception exception) =>
        exception is SshException
        || exception is SshConnectionException
        || exception is ProxyException
        || exception is ScpException
        || exception is InvalidOperationException
        || exception is ArgumentException
        || exception is IOException;

    private static void OpenInvalidConfigurationWindow(string reason, bool isPlaceholderPrompt = false)
    {
        if (isPlaceholderPrompt)
        {
            MessageBox.Show(
                "VisH has detected default placeholder paths/credentials.\n\nPlease update your settings with your actual local working directory, cluster username, and SSH key before continuing.",
                "Configuration Required",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        string message = isPlaceholderPrompt
            ? "Default placeholder paths detected. Please configure your settings below."
            : "Please create a valid configuration.";
        if (!string.IsNullOrWhiteSpace(reason)) message += "\n\n" + reason;
        new SettingsWindow(message).Show();
    }
    
    protected override void OnExit(ExitEventArgs e)
    {
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
