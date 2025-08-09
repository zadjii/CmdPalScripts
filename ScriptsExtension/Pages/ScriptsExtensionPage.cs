// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;

namespace ScriptsExtension;


internal sealed partial class ScriptsExtensionPage : ListPage
{
    private readonly SettingsModel _settings;

    public ScriptsExtensionPage(SettingsModel settings)
    {
        Icon = Icons.Logo;
        Title = "Scripts";
        Name = "Open";
        ShowDetails = true;
        _settings = settings;
    }

    public override IListItem[] GetItems()
    {
        var t = _settings.LoadAllAsync();
        t.Wait();

        var commandItems = GetAllCommandItems(_settings.Scripts, _settings);

        return commandItems.ToArray();
    }


    private static ListItem[] GetAllCommandItems(IEnumerable<ScriptMetadata> metadata, SettingsModel settings)
    {
        List<ListItem> commandItems = new();

        foreach (var script in metadata)
        {
            if (script == null || string.IsNullOrEmpty(script.Title))
            {
                continue;
            }

            ListItem commandItem = new DoScriptListItem(script, settings);

            commandItems.Add(commandItem);
        }

        return commandItems.ToArray();
    }
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
internal sealed partial class DoScriptCommand : InvokableCommand
{
    private ScriptMetadata Metadata { get; }

    private SettingsModel Settings { get; }

    public event TypedEventHandler<DoScriptCommand, InvokeRequestedArgs>? InvokeRequested;

    internal DoScriptCommand(ScriptMetadata metadata, SettingsModel settings)
    {
        Metadata = metadata;
        Settings = settings;
        Name = "Run script";
    }

    public override ICommandResult Invoke(object? sender)
    {
        var args = new InvokeRequestedArgs(Metadata);
        InvokeRequested?.Invoke(this, args);
        return args.Result ?? CommandResult.KeepOpen();
        //return Metadata.InvokeWithArgs(sender, [], Settings);
    }
}

internal sealed partial class DoScriptListItem : ListItem
{
    private ScriptMetadata _metadata { get; }
    private SettingsModel _settings { get; }
    public DoScriptListItem(ScriptMetadata script, SettingsModel settings)
    {
        _metadata = script;
        _settings = settings;
        var command = script.ToCommand(settings);
        Command = command;
        if (command is DoScriptCommand doScript)
        {
            doScript.InvokeRequested += InvokeScriptHandler;
        }
        Title = script.Title ?? string.Empty;
        Subtitle = script.PackageName ?? string.Empty;
        Icon = script.IconInfo;

        MarkdownPage scriptPage = new($"```\r\n{script.ScriptBody}\r\n```")
        {
            Title = Title,
            Icon = script.IconInfo,
            Name = "View script",
        };
        CommandContextItem viewScript = new(scriptPage)
        {
        };

        // Details = new Details() { Body = $"```\r\n{script.ScriptBody}\r\n```" },
        MoreCommands = [viewScript];
        Tags = [script.LanguageTag, new Tag(script.Mode.ToString())];
    }

    private void InvokeScriptHandler(DoScriptCommand sender, InvokeRequestedArgs args)
    {
        args.Result = InvokeWithArgs(_metadata, [], _settings);
    }


    public CommandResult InvokeWithArgs(ScriptMetadata script, string[] args, SettingsModel settings)
    {
        // Determine which exe to use to run this command
        string? exePath;
        string? exeArgs;
        if (script.Language.Equals("ps1", StringComparison.OrdinalIgnoreCase))
        {
            exePath = "pwsh.exe";
            exeArgs = $"-noprofile -nologo -File \"{script.ScriptFilePath}\"";
        }
        else if (script.Language.Equals("py", StringComparison.OrdinalIgnoreCase))
        {
            exePath = "python.exe";
            exeArgs = $"\"{script.ScriptFilePath}\"";
        }
        else
        {
            var pathFromSettings = settings.BashPath;

            // split it
            exePath = pathFromSettings.Split(' ').FirstOrDefault() ?? "bash";
            exeArgs = pathFromSettings.Substring(exePath.Length).Trim();
            exeArgs += $"-c \"{script.ScriptFilePath}\"";
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
        //var scriptDirectory = Path.GetDirectoryName(ScriptFilePath);

        switch (script.Mode)
        {
            case ScriptMode.FullOutput:
                // In `fullOutput` the entire output is presented on a separate view, similar to a terminal window. This is handy when your script generates output to consume.
                ShellHelpers.OpenInShell(
                    exePath,
                    exeArgs,
                    script.ActualScriptWorkingDir,
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
                            WorkingDirectory = script.ActualScriptWorkingDir,
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
                        var lastLine = output.Split(ScriptMetadata.Separator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
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
                            WorkingDirectory = script.ActualScriptWorkingDir,
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
                        lastLine = output.Split(ScriptMetadata.Separator, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
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
                            WorkingDirectory = script.ActualScriptWorkingDir,
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
                        var lastLine = output.Split(ScriptMetadata.SeparatorArray, StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
                        Subtitle = lastLine ?? string.Empty;
                    }

                    // TODO!
                    return CommandResult.KeepOpen();
                }
        }

        return CommandResult.KeepOpen();
    }
}

internal sealed class InvokeRequestedArgs(ScriptMetadata Script, CommandResult? Result = null)
{
    public ScriptMetadata Script { get; set; } = Script;
    public CommandResult? Result { get; set; } = Result;
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
[JsonSerializable(typeof(string))]
[JsonSerializable(typeof(bool))]
[JsonSerializable(typeof(SettingsModel))]
[JsonSerializable(typeof(ObservableCollection<string>), TypeInfoPropertyName = "StringObservableCollection")]
[JsonSerializable(typeof(List<string>), TypeInfoPropertyName = "StringList")]
[JsonSerializable(typeof(ScriptArgument), TypeInfoPropertyName = "ScriptArgument")]
[JsonSerializable(typeof(ScriptMetadata), TypeInfoPropertyName = "ScriptMetadata")]
[JsonSerializable(typeof(DropdownItem), TypeInfoPropertyName = "DropdownItem")]
[JsonSourceGenerationOptions(UseStringEnumConverter = true, WriteIndented = true, IncludeFields = true, PropertyNameCaseInsensitive = true, AllowTrailingCommas = true)]
internal sealed partial class JsonSerializationContext : JsonSerializerContext
{
}
