// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ScriptsExtension;


internal sealed partial class ScriptsExtensionPage : ListPage
{
    private readonly Settings _settings;

    public ScriptsExtensionPage(Settings settings)
    {
        Icon = Icons.Logo;
        Title = "Scripts";
        Name = "Open";
        ShowDetails = true;
        _settings = settings;
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
