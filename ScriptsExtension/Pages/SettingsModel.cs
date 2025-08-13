// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

namespace ScriptsExtension;

public sealed partial class SettingsModel : IDisposable
{
    // public string ScriptsPath { get; set; } = "d:\\dev\\script-commands-test\\windows-commands";

    [JsonIgnore]
    public static string FilePath { get; }

    public string BashPath { get; set; } = "wsl -- bash";

    public List<ScriptDirectoryInfo> Directories
    {
        get => _directories;
        set
        {
            if (_directories != value)
            {
                _directories = value;
                RefreshFileWatchers();
            }
        }
    }

    private List<ScriptDirectoryInfo> _directories = new();

    [JsonIgnore]
    public List<ScriptMetadata> Scripts { get; } = new();

    [JsonIgnore]
    private readonly List<FileSystemWatcher> _fileWatchers = new();


    [JsonIgnore]
    public bool Loaded { get; private set; }

    public event TypedEventHandler<SettingsModel, object>? DirectoriesChanged;
    public event TypedEventHandler<SettingsModel, object>? ScriptsChanged;
    public event TypedEventHandler<SettingsModel, object>? SettingsLoadCompleted;

    internal static string SettingsJsonPath()
    {
        var directory = Utilities.BaseSettingsPath("Scripts");
        Directory.CreateDirectory(directory);

        // now, the state is just next to the exe
        return Path.Combine(directory, "settings.json");
    }


    static SettingsModel()
    {
        FilePath = SettingsJsonPath();
    }

    [JsonConstructor]
    internal SettingsModel() { }

    public async Task LoadAllAsync()
    {
        DirectoriesChanged?.Invoke(this, EventArgs.Empty);

        Scripts.Clear();

        // Dispose existing watchers before creating new ones
        DisposeFileWatchers();

        foreach (var dir in Directories)
        {
            var files = await GetScriptFiles(dir.FullPath);

            var metadata = await GetAllScriptMetadata(files, this);

            foreach (var script in metadata.OrderBy(m => m.PackageName))
            {
                Scripts.Add(script);
            }

            // Set up file watcher for this directory
            SetupFileWatcher(dir.FullPath);

            ScriptsChanged?.Invoke(this, EventArgs.Empty);
        }
        Loaded = true;
        SettingsLoadCompleted?.Invoke(this, EventArgs.Empty);
    }


    private static async Task<string[]> GetScriptFiles(string scriptsPath)
    {
        if (string.IsNullOrEmpty(scriptsPath) || !Directory.Exists(scriptsPath))
        {
            return Array.Empty<string>();
        }

        // Get all script files in the directory and subdirectories
        // We are looking for .sh, .ps1, and .py files
        if (!Directory.Exists(scriptsPath))
        {
            return Array.Empty<string>();
        }

        var files = await Task.Run(
            () => Directory.GetFiles(scriptsPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            .ToArray()
            );

        return files;
    }

    private static async Task<ScriptMetadata?> GetScriptMetadata(string scriptFile)
    {
        if (string.IsNullOrEmpty(scriptFile) || !File.Exists(scriptFile))
        {
            return null;
        }

        var ext = Path.GetExtension(scriptFile).ToLowerInvariant();
        return await Task.Run(() => ext switch
        {
            ".sh" => ScriptMetadata.FromBash(scriptFile),
            ".ps1" => ScriptMetadata.FromPowershell(scriptFile),
            ".py" => ScriptMetadata.FromPython(scriptFile),
            _ => null,
        });
    }

    private static async Task<ScriptMetadata[]> GetAllScriptMetadata(string[] scriptFiles, SettingsModel settings)
    {
        List<ScriptMetadata> metadataList = new();

        foreach (var scriptFile in scriptFiles)
        {
            var metadata = await GetScriptMetadata(scriptFile);
            if (metadata != null && !string.IsNullOrEmpty(metadata.Title))
            {
                metadataList.Add(metadata);
            }
        }

        return metadataList.ToArray();
    }

    public static SettingsModel LoadSettings()
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException($"You must set a valid {nameof(SettingsModel.FilePath)} before calling {nameof(LoadSettings)}");
        }

        if (!File.Exists(FilePath))
        {
            Debug.WriteLine("The provided settings file does not exist");
            return new();
        }

        try
        {
            // Read the JSON content from the file
            var jsonContent = File.ReadAllText(FilePath);

            var loaded = JsonSerializer.Deserialize<SettingsModel>(jsonContent, JsonSerializationContext.Default.SettingsModel);

            Debug.WriteLine(loaded != null ? "Loaded settings file" : "Failed to parse");

            return loaded ?? new();
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }

        return new();
    }

    public static void SaveSettings(SettingsModel model)
    {
        if (string.IsNullOrEmpty(FilePath))
        {
            throw new InvalidOperationException($"You must set a valid {nameof(FilePath)} before calling {nameof(SaveSettings)}");
        }

        try
        {
            // Serialize the main dictionary to JSON and save it to the file
            var settingsJson = JsonSerializer.Serialize(model, JsonSerializationContext.Default.SettingsModel);

            // Is it valid JSON?
            if (JsonNode.Parse(settingsJson) is JsonObject newSettings)
            {
                // Now, read the existing content from the file
                var oldContent = File.Exists(FilePath) ? File.ReadAllText(FilePath) : "{}";

                // Is it valid JSON?
                if (JsonNode.Parse(oldContent) is JsonObject savedSettings)
                {
                    foreach (var item in newSettings)
                    {
                        savedSettings[item.Key] = item.Value?.DeepClone();
                    }

                    var serialized = savedSettings.ToJsonString(JsonSerializationContext.Default.Options);
                    File.WriteAllText(FilePath, serialized);

                    //// TODO: Instead of just raising the event here, we should
                    //// have a file change watcher on the settings file, and
                    //// reload the settings then
                    //model.SettingsChanged?.Invoke(model, null);
                }
                else
                {
                    Debug.WriteLine("Failed to parse settings file as JsonObject.");
                }
            }
            else
            {
                Debug.WriteLine("Failed to parse settings file as JsonObject.");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.ToString());
        }
    }

    private void SetupFileWatcher(string directoryPath)
    {
        if (string.IsNullOrEmpty(directoryPath) || !Directory.Exists(directoryPath))
        {
            return;
        }

        var watcher = new FileSystemWatcher(directoryPath)
        {
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.CreationTime | NotifyFilters.LastWrite,
            Filter = "*.*"
        };

        watcher.Created += OnFileChanged;
        watcher.Deleted += OnFileChanged;
        watcher.Renamed += OnFileRenamed;
        watcher.Changed += OnFileChanged;

        watcher.EnableRaisingEvents = true;
        _fileWatchers.Add(watcher);
    }

    private async void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            var extension = Path.GetExtension(e.FullPath).ToLowerInvariant();

            // Only process script files
            if (extension != ".sh" && extension != ".ps1" && extension != ".py")
            {
                return;
            }

            // Verify the file is within one of our monitored directories
            if (!IsFileInMonitoredDirectory(e.FullPath))
            {
                return;
            }

            if (e.ChangeType == WatcherChangeTypes.Created)
            {
                await AddScript(e.FullPath);
            }
            else if (e.ChangeType == WatcherChangeTypes.Deleted)
            {
                RemoveScript(e.FullPath);
            }
            else if (e.ChangeType == WatcherChangeTypes.Changed)
            {
                RemoveScript(e.FullPath);
                await AddScript(e.FullPath);
                SettingsLoadCompleted?.Invoke(this, EventArgs.Empty);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error handling file change event: {ex.Message}");
        }
    }

    private async void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        try
        {
            var oldExtension = Path.GetExtension(e.OldFullPath).ToLowerInvariant();
            var newExtension = Path.GetExtension(e.FullPath).ToLowerInvariant();

            // Remove old script if it was a script file and in monitored directory
            if ((oldExtension == ".sh" || oldExtension == ".ps1" || oldExtension == ".py") &&
                IsFileInMonitoredDirectory(e.OldFullPath))
            {
                RemoveScript(e.OldFullPath);
            }

            // Add new script if it's a script file and in monitored directory
            if ((newExtension == ".sh" || newExtension == ".ps1" || newExtension == ".py") &&
                IsFileInMonitoredDirectory(e.FullPath))
            {
                await AddScript(e.FullPath);
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error handling file rename event: {ex.Message}");
        }
    }

    private bool IsFileInMonitoredDirectory(string filePath)
    {
        return Directories.Any(dir =>
            filePath.StartsWith(dir.FullPath, StringComparison.OrdinalIgnoreCase));
    }

    private async Task AddScript(string scriptPath)
    {
        var metadata = await GetScriptMetadata(scriptPath);
        if (metadata != null && !string.IsNullOrEmpty(metadata.Title))
        {
            // Check if script already exists (avoid duplicates)
            var existingScript = Scripts.FirstOrDefault(s => string.Equals(s.ScriptFilePath, scriptPath, StringComparison.OrdinalIgnoreCase));
            if (existingScript == null)
            {
                // Find the correct position to insert to maintain sorted order
                var insertIndex = Scripts.BinarySearch(metadata, Comparer<ScriptMetadata>.Create((a, b) =>
                    string.Compare(a.PackageName, b.PackageName, StringComparison.OrdinalIgnoreCase)));

                if (insertIndex < 0)
                {
                    insertIndex = ~insertIndex;
                }

                Scripts.Insert(insertIndex, metadata);
                ScriptsChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    private void RemoveScript(string scriptPath)
    {
        var scriptToRemove = Scripts.FirstOrDefault(s => string.Equals(s.ScriptFilePath, scriptPath, StringComparison.OrdinalIgnoreCase));
        if (scriptToRemove != null)
        {
            Scripts.Remove(scriptToRemove);
            ScriptsChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void DisposeFileWatchers()
    {
        foreach (var watcher in _fileWatchers)
        {
            watcher.EnableRaisingEvents = false;
            watcher.Created -= OnFileChanged;
            watcher.Deleted -= OnFileChanged;
            watcher.Renamed -= OnFileRenamed;
            watcher.Changed -= OnFileChanged;
            watcher.Dispose();
        }
        _fileWatchers.Clear();
    }

    private void RefreshFileWatchers()
    {
        DisposeFileWatchers();

        foreach (var dir in Directories)
        {
            SetupFileWatcher(dir.FullPath);
        }
    }

    public void Dispose()
    {
        DisposeFileWatchers();
    }
}


public sealed class ScriptDirectoryInfo
{
    public string FullPath { get; set; } = string.Empty;
    [JsonIgnore]
    public string PathName => Path.GetFileName(FullPath);
    public ScriptDirectoryInfo() { }
    public ScriptDirectoryInfo(string fullPath)
    {
        FullPath = fullPath;
    }
}
