// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ScriptsExtension;

public sealed class SettingsModel
{
    // public string ScriptsPath { get; set; } = "d:\\dev\\script-commands-test\\windows-commands";

    [JsonIgnore]
    public static string FilePath { get; }

    public string BashPath { get; set; } = "wsl -- bash";

    public ObservableCollection<ScriptDirectoryInfo> Directories { get; set; } = new()
    {
        //new ScriptDirectoryInfo("d:\\dev\\script-commands-test\\windows-commands")
    };

    [JsonIgnore]
    public ObservableCollection<ScriptMetadata> Scripts { get; } = new();


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
        Scripts.Clear();

        foreach (var dir in Directories)
        {
            var files = await GetScriptFiles(dir.FullPath);

            var metadata = await GetAllScriptMetadata(files, this);

            foreach (var script in metadata.OrderBy(m => m.PackageName))
            {
                Scripts.Add(script);
            }
        }

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
