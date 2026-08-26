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
    

    public static Config Load()
    {
        string baseDir = AppContext.BaseDirectory;
        string configPath = Path.Combine(baseDir, "Data", "config.json");
        string jsonContent = File.ReadAllText(configPath);

        Config? cfg = JsonSerializer.Deserialize<Config>(jsonContent);
        
        if (cfg != null)
        {
            return cfg;
        }
        throw new InvalidOperationException("Konnte config.json nicht lesen.");
    }
}
