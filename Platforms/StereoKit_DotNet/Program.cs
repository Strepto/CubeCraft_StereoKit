using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using StereoKit;
using StereoKitApp;
using StereoKitApp.Utils;

namespace StereoKit_DotNet
{
    internal static class SKLoader
    {
        private static void Main(string[] args)
        {
            // This will allow the App constructor to call a few SK methods
            // before Initialize is called.
            SK.PreLoadLibrary();

            // If the app has a constructor that takes a string array, then
            // we'll use that, and pass the command line arguments into it on
            // creation
            Type appType = typeof(App);
            App? app =
                appType.GetConstructor(new Type[] { typeof(string[]) }) != null
                    ? (App)Activator.CreateInstance(appType, new object[] { args })!
                    : (App)Activator.CreateInstance(appType)!;
            if (app == null)
                throw new Exception("StereoKit loader couldn't construct an instance of the App!");

            // Initialize StereoKit, and the app
            var settings = App.Settings;

            if (!Path.IsPathRooted(settings.assetsFolder))
            {
                settings.assetsFolder = Path.Combine(
                    // Hack to make Assets load from correct folder when running in dotnet watch.
                    Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location
                    )!,
                    settings.assetsFolder
                );
            }

            if (!SK.Initialize(settings))
                Environment.Exit(1);
            IntPtr windowHandle = Process.GetCurrentProcess().MainWindowHandle;

            // if (MultiplayerDevUtils.IsInDesktopFlatMonitorMode())
            // {
            //     // Layout windows by window type. So they are side by side if you run two at a time.
            //     var clientBuildType = MultiplayerDevUtils.GetClientBuildType();
            //     if (clientBuildType == ClientBuild.Debug)
            //     {
            //         MoveWindow(windowHandle, 0, 0, 1920 / 2 - 5, 1080 - 200, true);
            //     }
            //     else if (clientBuildType == ClientBuild.Debug2)
            //     {
            //         MoveWindow(windowHandle, 1920 / 2, 0, 1920 / 2 - 5, 1080 - 200, true);
            //     }
            //
            //     SetWindowText(
            //         windowHandle,
            //         $"{Process.GetCurrentProcess().ProcessName} - {clientBuildType}"
            //     );
            // }

            app.Init();
            try
            {
                // Now loop until finished, and then shut down
                while (SK.Step(app.Step)) { }
            }
            finally
            {
                SK.Shutdown();
            }
        }

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool MoveWindow(
            IntPtr hWnd,
            int x,
            int y,
            int nWidth,
            int nHeight,
            bool bRepaint
        );

        [DllImport("user32.dll")]
        static extern int SetWindowText(IntPtr hWnd, string text);
    }
}
