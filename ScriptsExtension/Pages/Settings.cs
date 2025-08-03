// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;

namespace ScriptsExtension;

public sealed class Settings
{
    public string ScriptsPath { get; set; } = "d:\\dev\\script-commands-test\\windows-commands";

    public string BashPath { get; set; } = "wsl -- bash";

    public ObservableCollection<ScriptDirectoryInfo> Directories { get; set; } = new() {
        new ScriptDirectoryInfo("d:\\dev\\script-commands-test\\windows-commands")
    };

    public ObservableCollection<ScriptMetadata> Scripts { get; } = new();

    public void LoadAll()
    {
        Scripts.Clear();

        foreach (var dir in Directories)
        {
            var files = GetScriptFiles(dir.FullPath);

            var metadata = GetAllScriptMetadata(files, this);

            foreach (var script in metadata.OrderBy(m => m.PackageName))
            {
                Scripts.Add(script);
            }
        }
    }


    private static string[] GetScriptFiles(string scriptsPath)
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

        var files = Directory.GetFiles(scriptsPath, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".sh", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".ps1", StringComparison.OrdinalIgnoreCase) ||
                        f.EndsWith(".py", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        return files;
    }

    private static ScriptMetadata? GetScriptMetadata(string scriptFile)
    {
        if (string.IsNullOrEmpty(scriptFile) || !File.Exists(scriptFile))
        {
            return null;
        }

        var ext = Path.GetExtension(scriptFile).ToLowerInvariant();
        return ext switch
        {
            ".sh" => ScriptMetadata.FromBash(scriptFile),
            ".ps1" => ScriptMetadata.FromPowershell(scriptFile),
            ".py" => ScriptMetadata.FromPython(scriptFile),
            _ => null,
        };
    }

    private static ScriptMetadata[] GetAllScriptMetadata(string[] scriptFiles, Settings settings)
    {
        List<ScriptMetadata> metadataList = new();

        foreach (var scriptFile in scriptFiles)
        {
            var metadata = GetScriptMetadata(scriptFile);
            if (metadata != null)
            {
                metadataList.Add(metadata);
            }
        }

        return metadataList.ToArray();
    }
}


public sealed class ScriptDirectoryInfo
{
    public string FullPath { get; set; }
    public string PathName => Path.GetFileName(FullPath);
    public ScriptDirectoryInfo(string fullPath)
    {
        FullPath = fullPath;
    }
}
