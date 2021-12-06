using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Views;
using Android.Content;
using StereoKit;
using System;
using Android.Graphics;
using Java.Lang;
using System.Threading.Tasks;
using StereoKitApp;
using System.IO;
using Xamarin.Essentials;
using System.Collections.Generic;

namespace StereoKit_Android
{
    /// <summary>
    /// Helper to avoid mulitple dialogues for Read and Write
    /// </summary>
    public class ReadWriteStoragePermission : Xamarin.Essentials.Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions =>
            new List<(string androidPermission, bool isRuntime)>
            {
                (Android.Manifest.Permission.ReadExternalStorage, true),
                (Android.Manifest.Permission.WriteExternalStorage, true)
            }.ToArray();
    }

    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    [IntentFilter(
        new[] { Intent.ActionMain },
        Categories = new[] { "com.oculus.intent.category.VR", Intent.CategoryLauncher }
    )]
    public class MainActivity : AppCompatActivity, ISurfaceHolderCallback2
    {
        App app;
        View surface;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            JavaSystem.LoadLibrary("openxr_loader");
            JavaSystem.LoadLibrary("StereoKitC");

            // Set up a surface for StereoKit to draw on
            Window.TakeSurface(this);
            Window.SetFormat(Format.Unknown);
            surface = new View(this);
            SetContentView(surface);
            surface.RequestFocus();

            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);

            Run(Handle);
        }

        public override void OnRequestPermissionsResult(
            int requestCode,
            string[] permissions,
            [GeneratedEnum] Android.Content.PM.Permission[] grantResults
        )
        {
            Xamarin.Essentials.Platform.OnRequestPermissionsResult(
                requestCode,
                permissions,
                grantResults
            );
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        static bool running = false;
        async void Run(IntPtr activityHandle)
        {
            if (running)
                return;
            running = true;

            // TODO: Consider a nice startup requesting permissions instead of being approached by this..
            var storageStatus = await Permissions.CheckStatusAsync<ReadWriteStoragePermission>();
            if (!storageStatus.HasFlag(PermissionStatus.Granted))
            {
                var status = await Permissions.RequestAsync<ReadWriteStoragePermission>();
                if (!status.HasFlag(PermissionStatus.Granted))
                {
                    Console.WriteLine(
                        "Did NOT grant storage permissions. TODO: Implement handling of missing storage permissions"
                    );
                    Console.WriteLine("Status: " + status);
                }
            }

            await Task.Run(
                () =>
                {
                    // If the app has a constructor that takes a string array, then
                    // we'll use that, and pass the command line arguments into it on
                    // creation
                    Type appType = typeof(App);
                    app =
                        appType.GetConstructor(new Type[] { typeof(string[]) }) != null
                            ? (App)Activator.CreateInstance(
                                  appType,
                                  new object[] { new string[0] { } }
                              )
                            : (App)Activator.CreateInstance(appType);
                    if (app == null)
                        throw new System.Exception(
                            "StereoKit loader couldn't construct an instance of the App!"
                        );

                    // Working path stuff is untested.
                    var workingPath = System.IO.Path.Combine(
                        Android.OS.Environment.ExternalStorageDirectory.AbsolutePath,
                        Android.OS.Environment.DirectoryPictures,
                        "Cubes"
                    );

                    if (!Directory.Exists(workingPath))
                        Directory.CreateDirectory(workingPath);

                    App.WorkDirectory = new DirectoryInfo(workingPath);
                    // Initialize StereoKit, and the app
                    var settings = App.Settings;

                    settings.assetsFolder = "Assets";
                    settings.androidActivity = activityHandle;
                    if (!SK.Initialize(settings))
                        return;
                    app.Init();

                    // Now loop until finished, and then shut down
                    while (SK.Step(app.Step)) { }
                    SK.Shutdown();
                    Android.OS.Process.KillProcess(Android.OS.Process.MyPid());
                }
            );
        }

        // Events related to surface state changes
        public void SurfaceChanged(
            ISurfaceHolder holder,
            [GeneratedEnum] Format format,
            int width,
            int height
        ) => SK.SetWindow(holder.Surface.Handle);
        public void SurfaceCreated(ISurfaceHolder holder) => SK.SetWindow(holder.Surface.Handle);
        public void SurfaceDestroyed(ISurfaceHolder holder) => SK.SetWindow(IntPtr.Zero);
        public void SurfaceRedrawNeeded(ISurfaceHolder holder) { }
    }
}
