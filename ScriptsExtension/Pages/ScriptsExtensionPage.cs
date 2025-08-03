// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ScriptsExtension;


internal sealed partial class ScriptsExtensionPage : ListPage
{
    private readonly Settings _settings = new();

    public ScriptsExtensionPage()
    {
        Icon = Icons.Logo;
        Title = "Scripts";
        Name = "Open";
        ShowDetails = true;
    }

    public override IListItem[] GetItems()
    {
        var files = GetScriptFiles(_settings.ScriptsPath);
        var metadata = GetAllScriptMetadata(files, _settings);

        var commandItems = GetAllCommandItems(metadata.OrderBy(m => m.PackageName), _settings);

        return commandItems.ToArray();
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

    private static ListItem[] GetAllCommandItems(IEnumerable<ScriptMetadata> metadata, Settings settings)
    {
        List<ListItem> commandItems = new();

        foreach (var script in metadata)
        {
            if (script == null || string.IsNullOrEmpty(script.Title))
            {
                continue;
            }

            var command = script.ToCommand(settings);
            MarkdownPage scriptPage = new($"```\r\n{script.ScriptBody}\r\n```")
            {
                Title = script.Title,
                Icon = script.IconInfo,
                Name = "View script",
            };
            CommandContextItem viewScript = new(scriptPage)
            {
            };

            ListItem commandItem = new(command)
            {
                Title = script.Title,
                Subtitle = script.PackageName ?? string.Empty,
                Icon = script.IconInfo,

                // Details = new Details() { Body = $"```\r\n{script.ScriptBody}\r\n```" },
                MoreCommands = [viewScript],

                // Tags = script.Arguments
                //     .Where(arg => arg != null && !string.IsNullOrEmpty(arg.Placeholder))
                //     .Select(arg => new Tag(arg!.Placeholder))
                //     .ToArray(),
                Tags = [script.LanguageTag, new Tag(script.Mode.ToString())],
            };

            commandItems.Add(commandItem);
        }

        return commandItems.ToArray();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed class Settings
{
    public string ScriptsPath { get; set; } = "d:\\dev\\script-commands-test\\windows-commands";

    public string BashPath { get; set; } = "wsl -- bash";
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed partial class ScriptArgument
{
    public string Type { get; set; } = "text";

    public string Placeholder { get; set; } = string.Empty;

    public bool Optional { get; set; }

    public bool PercentEncoded { get; set; }

    public DropdownItem[]? Data { get; set; }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed partial class DropdownItem
{
    public string Title { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed partial class ScriptMetadata
{

    internal static readonly char[] Separator = new[] { '\n', '\r' };
    internal static readonly char[] SeparatorArray = new[] { '\n', '\r' };
    /*

    From the README

| Name                 | Description                                                                                                                                                                                                                                                                          | Required | App Version         |
|----------------------|--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|----------|---------------------|
|schemaVersion        | Schema version to prepare for future changes in the API. Currently there is only version 1 available.                                                                                                                                                                                | Yes      | 0.29+               |
| title                | Display name of the Script Command that is shown as title in the root search.                                                                                                                                                                                                        | Yes      | 0.29+               |
| mode                 | Specifies how the script is executed and how the output is presented. [Details of the options for this parameter can be viewed here](https://github.com/raycast/script-commands/blob/master/documentation/OUTPUTMODES.md) | Yes      | 0.29+               |
| packageName          | Display name of the package that is shown as subtitle in the root search. When not provided, the name will be inferred from the script directory name.                                                                                                                               | No       | 0.29+               |
| icon                 | Icon that is displayed in the root search. Can be an emoji, a file path (relative or full) or a remote URL (only https). Supported formats for images are PNG and JPEG. Please make sure to use small icons, recommended size - 64px.                                                | No       | 0.29+               |
| iconDark             | Same as `icon`, but for dark theme. If not specified, then `icon` will be used in both themes.                                                                                                                             | No       | 1.3.0+              |
| currentDirectoryPath | Path from which the script is executed. Default is the path of the script.                                                                                                                                                                                                           | No       | 0.29+               |
| needsConfirmation    | Specify `true` if you would like to show confirmation alert dialog before running the script. Can be helpful with destructive scripts like "Quit All Apps" or "Empty Trash". Default value is `false`.                                                                               | No       | 0.30+               |
| refreshTime          | Specify a refresh interval for inline mode scripts in seconds, minutes, hours or days. Examples: 10s, 1m, 12h, 1d. Note that the actual times can vary depending on how the OS prioritises scheduled work. The minimum refresh interval is 10 seconds. If you have more than 10 inline commands, only the first 10 will be refreshed automatically; the rest have to be manually refreshed by navigating to them and pressing `return`.| No       | 0.31+ |
| argument[1...3]      | [Custom arguments, see Passing Arguments page](https://github.com/raycast/script-commands/blob/master/documentation/ARGUMENTS.md) for detail of how to use this field | No | 1.2.0+ |
| author               | Define an author name to be part of the script commands documentation | No | |
| authorURL            | Author social media, website, email or anything to help the users to get in touch | No | |
| description          | A brief description about the script command to be presented in the documentation | No | |

    */
    public string? SchemaVersion { get; set; }

    public string? Title { get; set; }

    public ScriptMode Mode { get; set; }

    public string? PackageName { get; set; }

    public string? Icon { get; set; }

    public string? IconDark { get; set; }

    public string ScriptFilePath { get; set; } = string.Empty;

    public IconInfo IconInfo => new(
        new(ResolveIconPath(IconDark ?? Icon ?? string.Empty)),
        new(ResolveIconPath(Icon ?? IconDark ?? string.Empty)));

    public string? CurrentDirectoryPath { get; set; }

    public bool NeedsConfirmation { get; set; }

    public string? RefreshTime { get; set; }

    // max 3 arguments
    public ScriptArgument?[] Arguments { get; set; } = new ScriptArgument?[3];

    public string? Author { get; set; }

    public string? AuthorUrl { get; set; }

    public string? Description { get; set; }

    public string ScriptBody { get; set; } = string.Empty;

    public string Language { get; private set; } = string.Empty;

    public static Tag PowerShellTag { get; } = new("ps1") { Icon = Icons.Pwsh };

    public static Tag BashTag { get; } = new("bash") { Icon = Icons.Bash };

    public static Tag PythonTag { get; } = new("py") { Icon = Icons.Python };

    public Tag LanguageTag => Language switch
    {
        "ps1" => PowerShellTag,
        "py" => PythonTag,
        "bash" => BashTag,
        _ => new Tag() { Text = Language, Icon = Icons.DocumentInput },
    };

    private string ResolveIconPath(string iconPath)
    {
        if (string.IsNullOrEmpty(iconPath))
        {
            return iconPath;
        }

        // If it's an emoji, URL, or already an absolute path, return as-is
        if (iconPath.Length <= 2 || // Likely an emoji
            iconPath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            iconPath.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            Path.IsPathRooted(iconPath))
        {
            return iconPath;
        }

        // If it's a relative path and we have a script file path, resolve it
        if (!string.IsNullOrEmpty(ScriptFilePath))
        {
            var scriptDirectory = Path.GetDirectoryName(ScriptFilePath);
            if (!string.IsNullOrEmpty(scriptDirectory))
            {
                var resolvedPath = Path.Combine(scriptDirectory, iconPath);

                // Normalize the path and check if the file exists
                if (File.Exists(resolvedPath))
                {
                    return Path.GetFullPath(resolvedPath);
                }
            }
        }

        // Return the original path if we can't resolve it
        return iconPath;
    }

    private static ScriptArgument? ParseArgument(string argumentJson)
    {
        if (string.IsNullOrWhiteSpace(argumentJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ScriptArgument>(argumentJson, JsonSerializationContext.Default.ScriptArgument);
        }
        catch (JsonException)
        {
            // If JSON parsing fails, treat it as a simple text argument for backward compatibility
            return new ScriptArgument
            {
                Type = "text",
                Placeholder = argumentJson,
                Optional = false,
                PercentEncoded = false,
            };
        }
    }

    public static ScriptMetadata? FromHashComments(string bashFile, string language = "sh")
    {
        if (string.IsNullOrEmpty(bashFile) || !File.Exists(bashFile))
        {
            return null;
        }

        var text = File.ReadAllText(bashFile);

        // Now parse the file looking for the metadata
        // Metadata is in the form of:
        // # @raycast.schemaVersion 1
        // # @raycast.title My First Script
        var lines = text.Split(Separator, StringSplitOptions.RemoveEmptyEntries);
        ScriptMetadata metadata = new()
        {
            ScriptBody = text,
            Language = language,
            ScriptFilePath = bashFile,
        };
        foreach (var line in lines)
        {
            if (line.StartsWith("# @raycast.", StringComparison.InvariantCulture))
            {
                var parts = line.Substring(11).Split(' ', 2);
                if (parts.Length == 2)
                {
                    var key = parts[0].Trim();
                    var value = parts[1].Trim();

                    switch (key)
                    {
                        case "schemaVersion":
                            metadata.SchemaVersion = value;
                            break;
                        case "title":
                            metadata.Title = value;
                            break;
                        case "mode":
                            metadata.Mode = value switch
                            {
                                "fullOutput" => ScriptMode.FullOutput,
                                "compact" => ScriptMode.Compact,
                                "silent" => ScriptMode.Silent,
                                "inline" => ScriptMode.Inline,
                                _ => ScriptMode.FullOutput, // Default to FullOutput if unknown
                            };

                            break;
                        case "packageName":
                            metadata.PackageName = value;
                            break;
                        case "icon":
                            metadata.Icon = value;
                            break;
                        case "iconDark":
                            metadata.IconDark = value;
                            break;
                        case "currentDirectoryPath":
                            metadata.CurrentDirectoryPath = value;
                            break;
                        case "needsConfirmation":
                            metadata.NeedsConfirmation = bool.Parse(value);
                            break;
                        case "refreshTime":
                            metadata.RefreshTime = value;
                            break;

                        // case "argument":
                        //    if (metadata.Arguments == null)
                        //    {
                        //        metadata.Arguments = [];
                        //    }

                        // Array.Resize(ref metadata.Arguments, metadata.Arguments.Length + 1);
                        //    metadata.Arguments[^1] = value;
                        //    break;
                        case "author":
                            metadata.Author = value;
                            break;
                        case "authorURL":
                            metadata.AuthorUrl = value;
                            break;
                        case "description":
                            metadata.Description = value;
                            break;

                        case "argument1":
                            metadata.Arguments[0] = ParseArgument(value);
                            break;
                        case "argument2":
                            metadata.Arguments[1] = ParseArgument(value);
                            break;
                        case "argument3":
                            metadata.Arguments[2] = ParseArgument(value);
                            break;
                    }
                }
            }
        }

        return metadata;
    }

    public static ScriptMetadata? FromPowershell(string psFile)
    {
        return FromHashComments(psFile, "ps1");
    }

    public static ScriptMetadata? FromPython(string pyFile)
    {
        return FromHashComments(pyFile, "py");
    }

    public static ScriptMetadata? FromBash(string bashFile)
    {
        return FromHashComments(bashFile, "bash");
    }

    public ICommand ToCommand(Settings settings)
    {
        return new DoScriptCommand(this, settings);
    }

    public CommandResult InvokeWithArgs(object? sender, string[] args, Settings settings)
    {
        // Determine which exe to use to run this command
        string? exePath;
        string? exeArgs;
        if (Language.Equals("ps1", StringComparison.OrdinalIgnoreCase))
        {
            exePath = "pwsh.exe";
            exeArgs = $"-noprofile -nologo -File \"{ScriptFilePath}\"";
        }
        else if (Language.Equals("py", StringComparison.OrdinalIgnoreCase))
        {
            exePath = "python.exe";
            exeArgs = $"\"{ScriptFilePath}\"";
        }
        else
        {
            var pathFromSettings = settings.BashPath;

            // split it
            exePath = pathFromSettings.Split(' ').FirstOrDefault() ?? "bash";
            exeArgs = pathFromSettings.Substring(exePath.Length).Trim();
            exeArgs += $"-c \"{ScriptFilePath}\"";
        }

        // If we have arguments, append them
        if (// args != null &&
            args.Length > 0
            /* Metadata.Arguments != null */)
        {
            foreach (var s in args)
            //    foreach (ICommandArgument arg in args)
            {
                //    if (arg != null && arg.Value is string s && !string.IsNullOrEmpty(s))
                if (!string.IsNullOrEmpty(s))
                {
                    exeArgs += $" \"{s}\"";
                }
            }
        }

        // Run the script, in the directory that the script is in
        var scriptDirectory = Path.GetDirectoryName(ScriptFilePath);

        switch (Mode)
        {
            case ScriptMode.FullOutput:
                // In `fullOutput` the entire output is presented on a separate view, similar to a terminal window. This is handy when your script generates output to consume.
                ShellHelpers.OpenInShell(
                    exePath,
                    exeArgs,
                    scriptDirectory,
                    ShellHelpers.ShellRunAsType.None,
                    runWithHiddenWindow: false);
                return CommandResult.Dismiss();
            case ScriptMode.Compact:
                {
                    // In `compact` mode the last line of the standard output is shown in the toast

                    // Start the process and capture the output
                    System.Diagnostics.Process process = new()
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = exeArgs,
                            WorkingDirectory = scriptDirectory,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        },
                    };
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Show the last line of output in a toast
                    if (!string.IsNullOrEmpty(output))
                    {
                        var lastLine = output.Split(Separator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                        ToastStatusMessage toast = new(new StatusMessage() { Message = lastLine ?? "Script executed successfully." });
                        toast.Show();
                    }

                    return CommandResult.KeepOpen();
                }

            case ScriptMode.Silent:
                {
                    // In `silent` mode the last line (if exists) will be shown in overlaying HUD toast after Raycast window is closed

                    // Start the process and capture the output
                    System.Diagnostics.Process process = new()
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = exeArgs,
                            WorkingDirectory = scriptDirectory,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        },
                    };
                    process.Start();
                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();
                    string? lastLine = null;

                    // Show the last line of output in a toast
                    if (!string.IsNullOrEmpty(output))
                    {
                        lastLine = output.Split(Separator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    }

                    return CommandResult.ShowToast(lastLine ?? "Script executed successfully.");
                }

            case ScriptMode.Inline:
                {
                    // In `inline` mode, the first line of output will be directly shown in the command item and automatically refresh according to the specified `refreshTime`. Tip: Set your dashboard items as favorites via the action menu in Raycast.
                    // TODO! **NOTE:** `refreshTime` parameter is required for `inline` mode. When not specified, `compact` mode will be used instead.

                    // In `silent` mode the last line (if exists) will be shown in overlaying HUD toast after Raycast window is closed

                    // Start the process and capture the output
                    System.Diagnostics.Process process = new()
                    {
                        StartInfo = new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = exePath,
                            Arguments = exeArgs,
                            WorkingDirectory = scriptDirectory,
                            RedirectStandardOutput = true,
                            UseShellExecute = false,
                            CreateNoWindow = true,
                        },
                    };
                    process.Start();
                    process.WaitForExit();
                    var output = process.StandardOutput.ReadToEnd();

                    // Show the last line of output in a toast
                    if (!string.IsNullOrEmpty(output))
                    {
                        _ = output.Split(SeparatorArray, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                    }

                    // TODO!
                    return CommandResult.KeepOpen();
                }
        }

        return CommandResult.KeepOpen();
    }
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed partial class DoScriptCommand : InvokableCommand
{
    private ScriptMetadata Metadata { get; }

    private Settings Settings { get; }

    internal DoScriptCommand(ScriptMetadata metadata, Settings settings)
    {
        Metadata = metadata;
        Settings = settings;
        Name = "Run script";
    }

    public override ICommandResult Invoke(object? sender)
    {
        return Metadata.InvokeWithArgs(sender, [], Settings);
    }
}
//[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
//internal sealed partial class DoScriptCommand : InvokableWithParams
//{
//    private ScriptMetadata Metadata { get; }
//    private Settings Settings { get; }
//    internal static readonly char[] Separator = new[] { '\n', '\r' };
//    internal static readonly char[] SeparatorArray = new[] { '\n', '\r' };
//    internal DoScriptCommand(ScriptMetadata metadata, Settings settings)
//    {
//        Metadata = metadata;
//        Settings = settings;
//        Name = "Run script";
//        BuildParams();
//    }
//    public override ICommandResult InvokeWithArgs(object sender, ICommandArgument[] args)
//    {
//    }
//    private void BuildParams()
//    {
//        if (Metadata.Arguments == null)
//        {
//            return;
//        }
//        List<CommandParameter> parameters = new();
//        foreach (var arg in Metadata.Arguments)
//        {
//            if (arg == null ||
//                string.IsNullOrEmpty(arg.Placeholder) ||
//                arg.Type != "text")
//            {
//                continue;
//            }
//            var param = new CommandParameter(arg.Placeholder, !arg.Optional);
//            parameters.Add(param);
//        }
//        this.Parameters = parameters.ToArray();
//    }
//}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal sealed partial class MarkdownPage : ContentPage
{
    private readonly string _text = string.Empty;

    public MarkdownPage(string text)
    {
        _text = text;
        Name = "Open";
    }

    public override IContent[] GetContent() => [new MarkdownContent(_text)];
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
internal enum ScriptMode
{
    FullOutput,
    Compact,
    Silent,
    Inline,
}

[System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.MaintainabilityRules", "SA1402:File may only contain a single type", Justification = "meh")]
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(List<string>), TypeInfoPropertyName = "StringList")]
[JsonSerializable(typeof(ScriptArgument), TypeInfoPropertyName = "ScriptArgument")]
[JsonSerializable(typeof(ScriptMetadata), TypeInfoPropertyName = "ScriptMetadata")]
[JsonSerializable(typeof(DropdownItem), TypeInfoPropertyName = "DropdownItem")]
[JsonSourceGenerationOptions(UseStringEnumConverter = true, WriteIndented = true, IncludeFields = true, PropertyNameCaseInsensitive = true, AllowTrailingCommas = true)]
internal sealed partial class JsonSerializationContext : JsonSerializerContext
{
}
