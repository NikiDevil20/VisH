using System.IO;
using System.Text.Json;

namespace GaussianTool.Objects;

public class Config
{
    public  string LocalRechnungenPath { get; init; }
    public string ClusterRechnungenPath { get; init; }
    public string ClusterUsername { get; init; }


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