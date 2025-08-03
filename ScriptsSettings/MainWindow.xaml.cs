// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using ScriptsExtension;

namespace ScriptsSettings;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<ScriptDirectoryInfo> _directories = new();
    private readonly Settings _scriptSettings;
    public SettingsViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        _scriptSettings = ScriptsExtension.ScriptsExtensionCommandsProvider.ScriptSettings;
        _scriptSettings.LoadAll();
        ViewModel = new(_scriptSettings);
    }
}

public sealed class SettingsViewModel
{
    private readonly Settings _model;

    public ObservableCollection<ScriptDirectoryInfo> Directories => _model.Directories;
    public ObservableCollection<ScriptMetadata> Commands => _model.Scripts;

    public SettingsViewModel(Settings settings)
    {
        _model = settings;
    }
}