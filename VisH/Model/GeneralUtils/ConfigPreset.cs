namespace VisH.Model.GeneralUtils;

/// <summary>Settings represented by the Settings window and stored as a named preset.</summary>
public class ConfigPreset
{
    public string Name { get; set; } = string.Empty;
    public string LocalRechnungenPath { get; set; } = string.Empty;
    public string ClusterUsername { get; set; } = string.Empty;
    public string SshKeyPath { get; set; } = string.Empty;
}
