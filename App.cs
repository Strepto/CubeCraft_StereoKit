using System;
using System.IO;
using System.Net;
using StereoKit;
using System.Numerics;
using JetBrains.Annotations;
using StereoKitApp.Features.Avatar;
using StereoKitApp.HLRuffles;
using StereoKitApp.UIs;
using StereoKitApp.Utils;
using StereoKitApp.Painting;

namespace StereoKitApp
{
    public class App
    {
        private RufflesTransport _rufflesTransport = new RufflesTransport();

        public static DirectoryInfo WorkDirectory = new DirectoryInfo(
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Cubes")
        );

        [PublicAPI("Used by the Platform runners")]
        public static SKSettings Settings =>
            new SKSettings
            {
                appName = "StereoKit Template",
                assetsFolder = "Assets",
                displayPreference = DisplayMode.MixedReality,
            };

        private readonly RefWrapper<Pose> _cubePose = new RefWrapper<Pose>(
            new Pose(0, 0, -0.4f, Quat.Identity)
        );
        // private static readonly Vec3 UIStartPosition = new Vec3(0.5f, 0, -0.4f);
        // private Pose _uiPose = new Pose(UIStartPosition, Quat.LookAt(UIStartPosition, Vec3.Zero));
        private Model _cube = null!;
        private PaintingManager _paintingManager = null!;

        private readonly Matrix4x4 _floorTransform = Matrix.TS(
            new Vector3(0, -1.5f, 0),
            new Vector3(30, 0.1f, 30)
        );

        private Material? _floorMaterial;
        private ConnectionMenu _connectionMenu = null!;

        [UsedImplicitly]
        private AvatarSpawner _avatarSpawner = null!;

        [PublicAPI("Used by the Platform runners")]
        public void Init()
        {
            if (!WorkDirectory.Exists)
                WorkDirectory = Directory.CreateDirectory(WorkDirectory.FullName);

            UI.ColorScheme = Color.HSV(0.5f, 0.8f, 0.8f);
            SK.AddStepper(_rufflesTransport);

            // Create assets used by the app
            _cube = Model.FromMesh(
                Mesh.GenerateRoundedCube(Vec3.One * 0.1f, 0.02f),
                Default.MaterialUI
            );

            _paintingManager = new PaintingManager(WorkDirectory);

            Vec3 connectionMenuStartPosition = new Vec3(-0.5f, 0, -0.4f);
            Pose connectionMenuPose = new Pose(
                connectionMenuStartPosition,
                Quat.LookAt(connectionMenuStartPosition, Vec3.Zero)
            );

            _connectionMenu = new ConnectionMenu(ref connectionMenuPose);

            _floorMaterial = new Material(Shader.FromFile("floor.hlsl"))
            {
                Transparency = Transparency.Blend
            };

            _avatarSpawner = new AvatarSpawner();

            _paintingManager.InstantiatePainting("PaintTest");

            if (MultiplayerDevUtils.GetClientBuildType() == ClientBuild.Debug)
            {
                //Console.WriteLine("Creating Session");
                _rufflesTransport.CreateSession(6776);
            }
            else if (MultiplayerDevUtils.GetClientBuildType() == ClientBuild.Debug2)
            {
                Console.WriteLine("Connecting to localhost session");
                _rufflesTransport.JoinSession(new IPEndPoint(IPAddress.Loopback, 6776));
            }
            else if (MultiplayerDevUtils.GetClientBuildType() == ClientBuild.Debug3)
            {
                Console.WriteLine("Connecting to localhost session");
                // _rufflesTransport.JoinSession(
                //     new IPEndPoint(IPAddress.Parse("192.168.1.232"), 6776)
                // );
                _rufflesTransport.JoinSession(new IPEndPoint(IPAddress.Loopback, 6776));
            }

            Renderer.SkyTex = Tex.FromCubemapEquirectangular(
                "Env/Skybox1.hdr",
                out SphericalHarmonics _ // We use the default lighting. As the colors are more like exported.
            );
        }
        //
        // private static class MainMenuState
        // {
        //     public static bool ToggleOpen;
        //     public static string InputValue = "";
        // }

        [PublicAPI("Used by the Platform runners")]
        // ReSharper disable once CognitiveComplexity -- The main update loop
        public void Step()
        {
            UI.EnableFarInteract = false;
            if (SK.System.displayType == Display.Opaque)
            {
                Default.MeshCube.Draw(_floorMaterial, _floorTransform);
            }

            _paintingManager.Update();

            // if (UI.Handle("Cube", ref _cubePose.GetValueRef(), _cube.Bounds, true)) { }

            // ControllerUtils.ShowController(Handed.Left);
            // ControllerUtils.ShowController(Handed.Right);

            if (!_rufflesTransport.IsRunning)
                _connectionMenu.Update();
            // using (
            //     UIUtils.UIWindowScope(
            //         "Header",
            //         ref _uiPose,
            //         new Vec2(0.3f, 0.3f),
            //         UIWin.Normal,
            //         UIMove.FaceUser
            //     )
            // )
            // {
            //     if (_rufflesTransport.IsHost)
            //     {
            //         UI.Text("You are hosting.");
            //     }
            //     else if (
            //         _rufflesTransport.IsRunning
            //         && _rufflesTransport.ConnectionState == ConnectionState.Connected
            //     )
            //     {
            //         UI.Text("You are connected I think?");
            //     }
            //     else if (
            //         _rufflesTransport.IsRunning
            //         && _rufflesTransport.ConnectionState != ConnectionState.Connected
            //     )
            //     {
            //         UI.Text("Trying to connect to session.");
            //     }
            //
            //     UI.Text(MultiplayerDevUtils.GetClientBuildType() + " Client");
            //
            //     if (UI.Button("Expand!"))
            //     {
            //         MainMenuState.ToggleOpen = !MainMenuState.ToggleOpen;
            //     }
            //
            //     if (MainMenuState.ToggleOpen)
            //     {
            //         UI.Text("This is my new cool UI stuff!");
            //
            //         UI.Text("My cool message.");
            //
            //         if (UI.Input("MyInput", ref MainMenuState.InputValue))
            //         {
            //             UI.Text("Cool");
            //         }
            //     }
            // }
            // Save some battery while coding.
            //Thread.Sleep(1000 / 20);
        }
    }
}
