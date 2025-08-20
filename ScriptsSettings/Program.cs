// Copyright (c) Mike Griese
// Mike Griese licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Shmuelie.WinRTServer;
using Shmuelie.WinRTServer.CsWinRT;

namespace ScriptsSettings;

public class Program
{
    private static App? _app;
    [STAThread]
    public static void Main(string[] args)
    {

        if (args.Length > 0 && args[0] == "-RegisterProcessAsComServer")
        {
            // COM Server mode - run the extension server
            global::Shmuelie.WinRTServer.ComServer server = new();

            ManualResetEvent extensionDisposedEvent = new(false);

            // We are instantiating an extension instance once above, and returning it every time the callback in RegisterExtension below is called.
            // This makes sure that only one instance of ScriptsExtension is alive, which is returned every time the host asks for the IExtension object.
            // If you want to instantiate a new instance each time the host asks, create the new instance inside the delegate.
            ScriptsExtension extensionInstance = new(extensionDisposedEvent);
            server.RegisterClass<ScriptsExtension, IExtension>(() => extensionInstance);
            server.Start();

            // This will make the main thread wait until the event is signalled by the extension class.
            // Since we have single instance of the extension object, we exit as soon as it is disposed.
            extensionDisposedEvent.WaitOne();
            server.Stop();
            server.UnsafeDispose();
        }
        else
        {
            // Regular WinUI application mode
            WinRT.ComWrappersSupport.InitializeComWrappers();
            Application.Start((p) =>
            {
                var context = new DispatcherQueueSynchronizationContext(DispatcherQueue.GetForCurrentThread());
                SynchronizationContext.SetSynchronizationContext(context);
                _app = new App();
            });
        }
    }
}
