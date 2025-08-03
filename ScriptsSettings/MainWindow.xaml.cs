// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
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

    private void OpenScriptFile_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScriptMetadata script)
        {
            try
            {
                if (File.Exists(script.ScriptFilePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = script.ScriptFilePath,
                        UseShellExecute = true
                    });
                }
            }
            catch (System.Exception ex)
            {
                // Handle error - could show a message dialog or log
                System.Diagnostics.Debug.WriteLine($"Failed to open script file: {ex.Message}");
            }
        }
    }

    private void OpenScriptDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScriptMetadata script)
        {
            var directory = Path.GetDirectoryName(script.ScriptFilePath);
            //var directory = script.ScriptFilePath;
            if (directory == null)
            {
                return;
            }

            try
            {
                if (Directory.Exists(directory))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{directory}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (System.Exception ex)
            {
                // Handle error - could show a message dialog or log
                System.Diagnostics.Debug.WriteLine($"Failed to open directory: {ex.Message}");
            }
        }
    }

    private void OpenDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement element && element.DataContext is ScriptDirectoryInfo directory)
        {
            try
            {
                if (Directory.Exists(directory.FullPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = "explorer.exe",
                        Arguments = $"\"{directory.FullPath}\"",
                        UseShellExecute = true
                    });
                }
            }
            catch (System.Exception ex)
            {
                // Handle error - could show a message dialog or log
                System.Diagnostics.Debug.WriteLine($"Failed to open directory: {ex.Message}");
            }
        }
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