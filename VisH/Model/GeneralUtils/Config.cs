using System.IO;
using System.Text.Json;

namespace VisH.Model.GeneralUtils;

public class Config
{
    public const string PlaceholderUsername = "<ClusterUsername>";
    public const string PlaceholderLocalRechnungenPath = @"C:\Users\<Username>\Documents\Rechnungen";
    public const string PlaceholderSshKeyPath = @"C:\Users\<Username>\.ssh\id_ed25519";
    public const string PlaceholderClusterRechnungenPath = "/home/<ClusterUsername>/Rechnungen";

    public string LocalRechnungenPath { get; init; } = string.Empty;
    public string ClusterRechnungenPath { get; init; } = string.Empty;
    public string ClusterUsername { get; init; } = string.Empty;
    public string Cluster { get; init; } = string.Empty;
    public string Storage { get; init; } = string.Empty;
    public string SshKeyPath { get; init; } = string.Empty;

    public static string UserDataDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "VisH", "Data");

    public static string UserConfigPath => Path.Combine(UserDataDirectory, "config.json");

    public static bool IsPlaceholderValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        return (value.Contains('<') && value.Contains('>'))
            || value.Contains("<Username>", StringComparison.OrdinalIgnoreCase)
            || value.Contains("<ClusterUsername>", StringComparison.OrdinalIgnoreCase)
            || value.Contains("your_username", StringComparison.OrdinalIgnoreCase);
    }

    public bool ContainsPlaceholders(out string reason)
    {
        if (IsPlaceholderValue(LocalRechnungenPath))
        {
            reason = "The local Rechnungen directory contains placeholder text. Please select a valid local folder.";
            return true;
        }

        if (IsPlaceholderValue(ClusterUsername))
        {
            reason = "The cluster username contains placeholder text. Please enter your cluster username.";
            return true;
        }

        if (IsPlaceholderValue(SshKeyPath))
        {
            reason = "The SSH key path contains placeholder text. Please select a valid SSH key file.";
            return true;
        }

        if (IsPlaceholderValue(ClusterRechnungenPath))
        {
            reason = "The cluster Rechnungen path contains placeholder text.";
            return true;
        }

        reason = string.Empty;
        return false;
    }

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
