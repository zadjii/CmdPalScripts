// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace ScriptsExtension;

internal sealed partial class ScriptsExtensionPage : ListPage
{
    public ScriptsExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Scripts for Command Palette";
        Name = "Open";
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new NoOpCommand()) { Title = "TODO: Implement your extension here" }
        ];
    }
}
