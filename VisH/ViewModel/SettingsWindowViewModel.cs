using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Windows;
using Microsoft.Win32;
using VisH.Model.GeneralUtils;

namespace VisH.ViewModel;

public class SettingsWindowViewModel : ViewModelBase
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    private readonly string _dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
    private string _configurationName = string.Empty;
    private string _workingDirectory = string.Empty;
    private string _username = string.Empty;
    private string _sshKeyFilePath = string.Empty;
    private Config? _activeConfig;
    private ConfigPreset? _selectedConfiguration;

    public string StartupMessage { get; }
    public bool HasStartupMessage => !string.IsNullOrWhiteSpace(StartupMessage);

    public ObservableCollection<ConfigPreset> SavedConfigurations { get; } = new();

    public string ConfigurationName { get => _configurationName; set => SetProperty(ref _configurationName, value); }
    public string WorkingDirectory { get => _workingDirectory; set => SetProperty(ref _workingDirectory, value); }
    public string Username { get => _username; set => SetProperty(ref _username, value); }
    public string SshKeyFilePath { get => _sshKeyFilePath; set => SetProperty(ref _sshKeyFilePath, value); }

    public ConfigPreset? SelectedConfiguration
    {
        get => _selectedConfiguration;
        set
        {
            if (!SetProperty(ref _selectedConfiguration, value) || value == null) return;
            ConfigurationName = value.Name;
            WorkingDirectory = value.LocalRechnungenPath;
            Username = value.ClusterUsername;
            SshKeyFilePath = value.SshKeyPath;
            ApplyConfiguration();
        }
    }

    public RelayCommand BrowseDirectoryCommand { get; }
    public RelayCommand BrowseSshKeyCommand { get; }
    public RelayCommand SaveConfigurationCommand { get; }
    public RelayCommand ApplyConfigurationCommand { get; }

    private string ConfigFilePath => Path.Combine(_dataDirectory, "config.json");

    public SettingsWindowViewModel(string startupMessage = "")
    {
        StartupMessage = startupMessage;
        BrowseDirectoryCommand = new RelayCommand(_ => BrowseDirectory());
        BrowseSshKeyCommand = new RelayCommand(_ => BrowseSshKey());
        SaveConfigurationCommand = new RelayCommand(_ => SaveConfiguration());
        ApplyConfigurationCommand = new RelayCommand(_ => ApplyConfiguration());

        Directory.CreateDirectory(_dataDirectory);
        _activeConfig = ReadConfig();
        WorkingDirectory = _activeConfig.LocalRechnungenPath;
        Username = _activeConfig.ClusterUsername;
        SshKeyFilePath = _activeConfig.SshKeyPath;
        LoadPresets();
    }

    private Config ReadConfig()
    {
        if (!File.Exists(ConfigFilePath)) return new Config();
        try { return JsonSerializer.Deserialize<Config>(File.ReadAllText(ConfigFilePath)) ?? new Config(); }
        catch (JsonException) { return new Config(); }
    }

    private void LoadPresets()
    {
        foreach (string file in Directory.EnumerateFiles(_dataDirectory, "*.json"))
        {
            if (Path.GetFileName(file).Equals("config.json", StringComparison.OrdinalIgnoreCase)) continue;
            try
            {
                ConfigPreset? preset = JsonSerializer.Deserialize<ConfigPreset>(File.ReadAllText(file));
                if (preset != null && !string.IsNullOrWhiteSpace(preset.Name)) SavedConfigurations.Add(preset);
            }
            catch (JsonException) { /* Ignore unrelated JSON files. */ }
        }
    }

    private void ApplyConfiguration()
    {
        if (!TryBuildConfig(out Config? config)) return;
        _activeConfig = config;
        WriteJson(ConfigFilePath, config);
    }

    private void SaveConfiguration()
    {
        if (!TryBuildConfig(out Config? config) || !TryGetPresetPath(out string path)) return;
        ConfigPreset preset = new()
        {
            Name = ConfigurationName.Trim(),
            LocalRechnungenPath = WorkingDirectory.Trim(),
            ClusterUsername = Username.Trim(),
            SshKeyPath = SshKeyFilePath.Trim()
        };
        WriteJson(path, preset);
        _activeConfig = config;
        WriteJson(ConfigFilePath, config);

        ConfigPreset? old = SavedConfigurations.FirstOrDefault(p => p.Name.Equals(preset.Name, StringComparison.OrdinalIgnoreCase));
        if (old == null) SavedConfigurations.Add(preset);
        else SavedConfigurations[SavedConfigurations.IndexOf(old)] = preset;
        SelectedConfiguration = preset;
    }

    private bool TryBuildConfig(out Config? config)
    {
        config = null;
        if (string.IsNullOrWhiteSpace(WorkingDirectory) || string.IsNullOrWhiteSpace(Username))
        {
            MessageBox.Show("Working directory and username are required.", "Invalid configuration", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }

        Config current = _activeConfig ?? ReadConfig();
        string username = Username.Trim();
        config = new Config
        {
            LocalRechnungenPath = WorkingDirectory.Trim(),
            ClusterUsername = username,
            ClusterRechnungenPath = $"/home/{username}/Rechnungen",
            SshKeyPath = SshKeyFilePath.Trim(),
            Cluster = current.Cluster,
            Storage = current.Storage
        };
        return true;
    }

    private bool TryGetPresetPath(out string path)
    {
        path = string.Empty;
        string name = ConfigurationName.Trim();
        if (string.IsNullOrWhiteSpace(name) || name is "." or ".." || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            MessageBox.Show("Enter a valid configuration name.", "Invalid configuration name", MessageBoxButton.OK, MessageBoxImage.Warning);
            return false;
        }
        path = Path.Combine(_dataDirectory, name + ".json");
        return true;
    }

    private void WriteJson<T>(string path, T value) => File.WriteAllText(path, JsonSerializer.Serialize(value, _jsonOptions));

    private void BrowseDirectory()
    {
        OpenFolderDialog dialog = new()
        {
            Title = "Select working directory",
            InitialDirectory = Directory.Exists(WorkingDirectory) ? WorkingDirectory : null
        };
        if (dialog.ShowDialog() == true) WorkingDirectory = dialog.FolderName;
    }

    private void BrowseSshKey()
    {
        OpenFileDialog dialog = new() { Title = "Select SSH key file", FileName = SshKeyFilePath };
        if (dialog.ShowDialog() == true) SshKeyFilePath = dialog.FileName;
    }
}
