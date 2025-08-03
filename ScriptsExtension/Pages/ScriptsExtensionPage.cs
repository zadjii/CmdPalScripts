// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

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
        _settings.LoadAll();

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

    private SettingsModel Settings { get; }

    internal DoScriptCommand(ScriptMetadata metadata, SettingsModel settings)
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
