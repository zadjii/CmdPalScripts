// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ScriptsSettings.Models;
using Windows.Storage.Pickers;

namespace ScriptsSettings;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly ObservableCollection<ScriptDirectoryInfo> _directories = new();
    private readonly SettingsModel _scriptSettings;
    public SettingsViewModel ViewModel { get; }

    public MainWindow()
    {
        InitializeComponent();

        _scriptSettings = ScriptsExtensionCommandsProvider.ScriptSettings;
        DispatcherQueue.TryEnqueue(async () => await _scriptSettings.LoadAllAsync());
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

    private async void AddDirectory_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folderPicker = new FolderPicker();

            // Initialize the folder picker with the window handle
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            WinRT.Interop.InitializeWithWindow.Initialize(folderPicker, hWnd);

            // Configure the folder picker
            folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
            folderPicker.FileTypeFilter.Add("*");

            // Show the folder picker
            var folder = await folderPicker.PickSingleFolderAsync().AsTask();

            if (folder != null)
            {
                var directoryPath = folder.Path;

                // Check if this directory is already in the list
                if (!_scriptSettings.Directories.Any(d => string.Equals(d.FullPath, directoryPath, StringComparison.OrdinalIgnoreCase)))
                {
                    // Add the new directory to the settings
                    var newDirectory = new ScriptDirectoryInfo(directoryPath);
                    _scriptSettings.Directories.Add(newDirectory);

                    // Save the settings
                    SettingsModel.SaveSettings(_scriptSettings);

                    // Reload all scripts to include any from the new directory
                    await _scriptSettings.LoadAllAsync();
                }
            }
        }
        catch (System.Exception ex)
        {
            // Handle error - could show a message dialog or log
            System.Diagnostics.Debug.WriteLine($"Failed to add directory: {ex.Message}");
        }
    }

    private void DirectoriesListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Show/hide the remove button based on whether a directory is selected
        RemoveDirectoryButton.Visibility = DirectoriesListView.SelectedItem != null
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private async void RemoveDirectory_Click(object sender, RoutedEventArgs e)
    {
        if (DirectoriesListView.SelectedItem is ScriptDirectoryInfo selectedDirectory)
        {
            try
            {
                // Remove the directory from the settings
                _scriptSettings.Directories.Remove(selectedDirectory);

                // Save the settings
                SettingsModel.SaveSettings(_scriptSettings);

                // Reload all scripts to remove any from the deleted directory
                await _scriptSettings.LoadAllAsync();

                // Clear the selection to hide the remove button
                DirectoriesListView.SelectedItem = null;
            }
            catch (System.Exception ex)
            {
                // Handle error - could show a message dialog or log
                System.Diagnostics.Debug.WriteLine($"Failed to remove directory: {ex.Message}");
            }
        }
    }

    private async void CommandsListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CommandsListView.SelectedItem is ScriptMetadata selectedScript)
        {
            try
            {
                if (File.Exists(selectedScript.ScriptFilePath))
                {
                    var content = await File.ReadAllTextAsync(selectedScript.ScriptFilePath);
                    ScriptContentTextBlock.Text = content;
                }
                else
                {
                    ScriptContentTextBlock.Text = "Script file not found.";
                }
            }
            catch (Exception ex)
            {
                ScriptContentTextBlock.Text = $"Error reading script file: {ex.Message}";
            }
        }
        else
        {
            ScriptContentTextBlock.Text = "Select a command to view its content";
        }
    }
}

public sealed class SettingsViewModel
{
    private readonly SettingsModel _model;
    private readonly DispatcherQueue _dispatcher;
    public ObservableCollection<ScriptDirectoryInfo> Directories { get; } // => _model.Directories;
    //public ObservableCollection<ScriptMetadata> Commands => _model.Scripts;
    public ObservableCollection<Package> Packages { get; }

    public SettingsViewModel(SettingsModel settings)
    {
        _model = settings;
        _dispatcher = DispatcherQueue.GetForCurrentThread();
        Directories = new ObservableCollection<ScriptDirectoryInfo>(_model.Directories);

        var names = _model.Scripts.Select(s => s.PackageName).ToHashSet();
        Packages = new ObservableCollection<Package>(names.Select(n => new Package(n!, _model)));

        // Initialize package commands
        foreach (var package in Packages)
        {
            package.UpdateCommands();
        }

        //_model.Scripts.CollectionChanged += (s, e) =>
        _model.ScriptsChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                UpdateScripts();
            });
        };

        _model.DirectoriesChanged += (s, e) =>
        {
            _dispatcher.TryEnqueue(() =>
            {
                UpdateDirectories();
                UpdateScripts();
            });
        };
    }

    private void UpdateScripts()
    {
        // update Packages to match the change
        var names = _model.Scripts.Select(s => s.PackageName).ToHashSet();
        Packages.Clear();
        foreach (var name in names)
        {
            var package = new Package(name!, _model);
            package.UpdateCommands();
            Packages.Add(package);
        }
    }
    private void UpdateDirectories()
    {
        Directories.Clear();
        foreach (var item in _model.Directories)
        {
            Directories.Add(item);
        }
    }
}

public sealed class Package(string name, SettingsModel Model)
{
    public string Name => name;
    public ObservableCollection<ScriptMetadata> Commands { get; } = new();
    public int NumCommands => Commands.Count;

    public void UpdateCommands()
    {
        Commands.Clear();
        foreach (var script in Model.Scripts.Where(s => s.PackageName == name))
        {
            Commands.Add(script);
        }
    }
}

public sealed partial class PackageTreeTemplateSelector : DataTemplateSelector
{
    public DataTemplate? PackageTemplate { get; set; }
    public DataTemplate? CommandTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore(object item, DependencyObject container)
    {
        return item switch
        {
            Package => PackageTemplate,
            ScriptMetadata => CommandTemplate,
            _ => base.SelectTemplateCore(item, container)
        };
    }
}