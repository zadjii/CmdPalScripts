// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.IO;
using Microsoft.UI.Xaml;

namespace ScriptsSettings;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<ScriptDirectoryInfo> _directories = new();

    public MainWindow()
    {
        InitializeComponent();

        _directories.Add(new ScriptDirectoryInfo("d:\\dev\\script-commands-test\\windows-commands"));
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
