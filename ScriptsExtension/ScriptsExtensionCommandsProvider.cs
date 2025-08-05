// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ScriptsExtension;

public partial class ScriptsExtensionCommandsProvider : CommandProvider
{
    private readonly List<ICommandItem> _commands;
    private readonly ScriptsExtensionPage _scriptsPage;
    public static SettingsModel ScriptSettings { get; } // = new();

    static ScriptsExtensionCommandsProvider()
    {
        ScriptSettings = SettingsModel.LoadSettings();
    }

    public ScriptsExtensionCommandsProvider()
    {
        DisplayName = "Scripts for Command Palette";
        Icon = Icons.Logo;
        
        var t = ScriptSettings.LoadAllAsync();
        t.Wait();

        _scriptsPage = new ScriptsExtensionPage(ScriptSettings);

        _commands = [
            new CommandItem(_scriptsPage) { Title = "Script commands" },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        List<ICommandItem> commands = [.. _commands];
        var topLevelScripts = _scriptsPage.GetItems();
        commands.InsertRange(0, topLevelScripts);
        return commands.ToArray();
    }

}
