// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;

namespace ScriptsExtension;

public sealed class Settings
{
    public string ScriptsPath { get; set; } = "d:\\dev\\script-commands-test\\windows-commands";

    public string BashPath { get; set; } = "wsl -- bash";

    public ObservableCollection<ScriptDirectoryInfo> Directories { get; set; } = new() {
        new ScriptDirectoryInfo("d:\\dev\\script-commands-test\\windows-commands")
    };
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
