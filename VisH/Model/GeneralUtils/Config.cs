using System.IO;
using System.Text.Json;

namespace VisH.Model.GeneralUtils;

public class Config
{
    public string LocalRechnungenPath { get; init; } = string.Empty;
    public string ClusterRechnungenPath { get; init; } = string.Empty;
    public string ClusterUsername { get; init; } = string.Empty;
    public string Cluster { get; init; } = string.Empty;
    public string Storage { get; init; } = string.Empty;
    public string SshKeyPath { get; init; } = string.Empty;

    public static string UserDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VisH", "Data");

    public static string UserConfigPath => Path.Combine(UserDataDirectory, "config.json");

    public static Config Load()
    {
        Directory.CreateDirectory(UserDataDirectory);
        EnsureUserConfigExists();
        string jsonContent = File.ReadAllText(UserConfigPath);

        Config? cfg = JsonSerializer.Deserialize<Config>(jsonContent);
        if (cfg != null) return cfg;
        throw new InvalidOperationException("Konnte config.json nicht lesen.");
    }

    private static void EnsureUserConfigExists()
    {
        if (File.Exists(UserConfigPath)) return;

        string bundledConfigPath = Path.Combine(AppContext.BaseDirectory, "Data", "config.json");
        if (!File.Exists(bundledConfigPath))
            throw new FileNotFoundException("Es wurde weder eine Benutzerkonfiguration noch eine Standardkonfiguration gefunden.", bundledConfigPath);

        File.Copy(bundledConfigPath, UserConfigPath);
    }
}
