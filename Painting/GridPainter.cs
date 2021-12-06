using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using StereoKit;
using StereoKit.Framework;
using StereoKitApp.Features.Gltf;
using StereoKitApp.Utils;
using StereoKitApp.Utils.Sounds;
using StereoKitApp.Utils.WordSequenceGenerator;
using Material = StereoKit.Material;
using Mesh = StereoKit.Mesh;

namespace StereoKitApp.Painting
{
    public class GridPainter
    {
        public Action? OpenModelManager; // Required.

        public PaintingManager.PaintingInstance? ActivePainting
        {
            get => _activePainting;
            set
            {
                Console.WriteLine("Set active painting");
                _activePainting = value;
                _doNotPaintUntilPinchRelease = true; // Used to avoid starting painting until hands unpinched.
            }
        }

        public Box3DModelData Box3DModelData => _activePainting.Box3DModelData;

        //private readonly SyncRef<Pose> _paintingRootSync = new SyncRef<Pose>(
        //    91289412,
        //    new Pose(0, 0, -0.3f, Quat.Identity)
        //);

        private readonly SoundSequencer _cubeCreateSoundSequencer;

        private readonly Vec3[] _gridPositions;

        private readonly uint _xyzDimensions;
        private readonly Model _gridCubeModel;
        private readonly Model _paletteColorModel;
        private readonly Model _menuBoard = AssetLookup.Models.ColorPaletteBoard;
        private readonly Model _colorPickerBoard = AssetLookup.Models.ColorPickerBoard;
        private readonly Model _paintButtonNormal = AssetLookup.Models.PaintButtonNormal;
        private readonly Model _paintButtonPushed = AssetLookup.Models.PaintButtonPushed;
        private readonly Model _eraseButtonNormal = AssetLookup.Models.EraseButtonNormal;
        private readonly Model _eraseButtonPushed = AssetLookup.Models.EraseButtonPushed;
        private readonly Model _colorPickerButtonNormal =
            AssetLookup.Models.ColorPickerButtonNormal;
        private readonly Model _colorPickerButtonPushed =
            AssetLookup.Models.ColorPickerButtonPushed;
        private readonly Model _saveButton = AssetLookup.Models.SaveButton;
        private readonly Model _loadButton = AssetLookup.Models.LoadButton;

        private readonly List<Model> _nukeButtons = new List<Model>()
        {
            AssetLookup.Models.NukeButton1,
            AssetLookup.Models.NukeButton2,
            AssetLookup.Models.NukeButton3
        };

        private float _initialPinchDistance = 0;
        private float _initialScale = 0;

        private static readonly Vec3 PaletteWindowInitialPosition = new Vec3(-0.4f, 0, -0.2f);

        private Pose _paletteWindowPose = new Pose(
            PaletteWindowInitialPosition,
            Quat.LookAt(PaletteWindowInitialPosition, Vec3.Zero)
        );

        private readonly Model _xMarksTheSpot;
        private readonly Model _colorPickerSphere;
        private readonly Model _colorPickerInside;
        private Box3DModelData.VoxelKind _currentVoxelKind = Box3DModelData.VoxelKind.Cube;
        private PaintingManager.PaintingInstance _activePainting;
        private bool _doNotPaintUntilPinchRelease;
        private ImageRadialMenu? _handMenu;

        public GridPainter(uint xyzDimensions = 16)
        {
            Console.WriteLine("I was here " + 123);
            _xyzDimensions = xyzDimensions;

            _gridPositions = GridUtils.GetGridLayout(xyzDimensions);
            _xMarksTheSpot = AssetLookup.Models.LoadX;
            foreach (var modelNode in _xMarksTheSpot.Nodes)
            {
                // Make visible through everything.
                // Kinda XRay material stuff
                modelNode.Material.DepthTest = DepthTest.Always;
                modelNode.Material.Transparency = Transparency.Add;
                modelNode.Material.DepthWrite = false;
                modelNode.Material.FaceCull = Cull.Back;
            }

            var xRayMaterial = Material.Default.Copy();
            xRayMaterial.DepthTest = DepthTest.Always;
            xRayMaterial.Transparency = Transparency.Add;
            xRayMaterial.DepthWrite = false;
            xRayMaterial.FaceCull = Cull.Back;

            var colorPickerMaterial = Material.Default.Copy();
            colorPickerMaterial.DepthTest = DepthTest.Always;
            colorPickerMaterial.Transparency = Transparency.Add;
            colorPickerMaterial.DepthWrite = false;
            colorPickerMaterial.FaceCull = Cull.Back;

            _colorPickerSphere = AssetLookup.Models.LoadColorPickerShell;
            _colorPickerSphere.Visuals[1].Material = xRayMaterial;
            _colorPickerInside = AssetLookup.Models.LoadColorPickerInside;
            _colorPickerInside.Visuals[0].Material = xRayMaterial;

            var materialWithTransparency = Material.Default.Copy();
            materialWithTransparency.Transparency = Transparency.Blend;
            _cubeCreateSoundSequencer = new SoundSequencer(
                AssetLookup.Sounds.UI.Pips,
                SoundSequencer.LoopingMode.PingPong
            );
            _gridCubeModel = Model.FromMesh(Mesh.Cube, materialWithTransparency);
            _paletteColorModel = AssetLookup.Models.RoundedButton;
            _paletteColorModel.Visuals[0].Material = materialWithTransparency;

            // _menuBoard.Visuals[2].Material = materialWithTransparency.Copy();

            _handMenu = new ImageRadialMenu(
                new HandRadialLayer(
                    "Models",
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.Cube)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.Rounded)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.RoundedEdge)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.Sliced)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.RoundedTop)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.Half)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.Chipped)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.CutEdge)
                    ),
                    new HandMenuItem(
                        "",
                        AssetLookup.Sprites.LoadRocketSprite,
                        () => OnPickedVoxelKind(Box3DModelData.VoxelKind.CutTop)
                    )
                )
            //new HandRadialLayer(
            //    "Layer2",
            //    new HandMenuItem(
            //        "Item3",
            //        AssetLookup.Sprites.LoadRocketSprite,
            //        () => Console.WriteLine("Rocket 🚀")
            //    ),
            //    new HandMenuItem(
            //        "Item4",
            //        AssetLookup.Sprites.LoadRocketSprite,
            //        () => Console.WriteLine("Cool 🍌")
            //    )
            //)
            );

            Console.WriteLine("I was here " + 124);
            SK.AddStepper(_handMenu);
        }

        private void OnPickedVoxelKind(Box3DModelData.VoxelKind voxelKind)
        {
            _currentVoxelKind = voxelKind;
            PaletteWindowState.SelectedTool = PaintingTool.Paint;
        }

        public enum PaintingTool
        {
            /// <summary>
            /// None: No active tool.
            /// </summary>
            None,
            Paint,
            Delete,
            ColorPick
        }

        public static class PaletteWindowState
        {
            public static PaintingTool SelectedTool = PaintingTool.Paint;

            public static float Saturation = 1;
            public static float Value = 1;
            public static float Hue = 1;

            private static int GetColorHash(float hue, float saturation, float value)
            {
                int hash = 17;
                hash = hash * 23 + hue.GetHashCode();
                hash = hash * 23 + saturation.GetHashCode();
                hash = hash * 23 + value.GetHashCode();
                return hash;
            }

            private static int colorHash = GetColorHash(Hue, Saturation, Value);
            private static Color _currentColor = Color.HSV(Hue, Saturation, Value);

            public static Color SelectedColor()
            {
                var newHash = GetColorHash(Hue, Saturation, Value);
                var color =
                    colorHash == newHash
                        ? _currentColor
                        : _currentColor = Color.HSV(Hue, Saturation, Value);

                colorHash = newHash;
                return color;
            }

            public static int PressesUntilDeleteAll { get; set; } = 0;

            public static bool Frozen = false;
            public static float Time = -1;

            /// <summary>
            /// Set the Hue, Saturation and Value fields from a color sample.
            /// </summary>
            /// <param name="color"></param>
            public static void SetHSVFromColor(Color color)
            {
                var hsv = color.ToHSV();
                Hue = hsv.x;
                Saturation = hsv.y;
                Value = hsv.z;
            }
        }

        public void Update()
        {
            if (ActivePainting == null)
            {
                UpdatePaletteUI();
                return;
            }

            UpdatePaletteUI();

            //ref var paintingRootPoseSync = ref _paintingRootSync.GetRef();
            var mainHand = Input.Hand(Handed.Right);
            var offHand = Input.Hand(Handed.Left);

            if (
                UI.IsInteracting(mainHand.handed)
                && mainHand.IsPinched
                && !_doNotPaintUntilPinchRelease
            )
            {
                // Avoid dragging after interacting with UI
                _doNotPaintUntilPinchRelease = true;
            }

            using (UIUtils.EnableFarInteractScope(false))
            {
                var gridScale = ActivePainting.Scale;

                var offsetMatrix = Matrix.TS(Vec3.Zero, gridScale);

                // UI.ShowVolumes = true; // Uncomment this to debug the code below.
                var handleBounds = new Bounds(Vec3.Zero); // Non-interactable if not offhand is used.
                if (
                    offHand.IsTracked
                    && (
                        offHand.pinchActivation > 0.6f || offHand.IsJustPinched || offHand.IsPinched
                    )
                )
                {
                    // Hack to allow movement only with off-hand.
                    var dim =
                        _activePainting.Box3DModelData.ActivePixelDatasBounds.dimensions
                        * gridScale;
                    handleBounds = new Bounds(
                        offsetMatrix.Translation
                            + _activePainting.Box3DModelData.ActivePixelDatasBounds.center
                                * gridScale,
                        dim
                    );
                }

                _gridCubeModel.Draw(Matrix.S(0.01f) * ActivePainting.Pose.ToMatrix(), Color.White);

                using (
                    UIUtils.UIHandleScope(
                        "painting",
                        ref ActivePainting.Pose,
                        handleBounds,
                        false,
                        UIMove.Exact,
                        out var wasHandleInteractedWith
                    )
                )
                {
                    // Scaling (if we use the offhand before the main hand)!
                    if (wasHandleInteractedWith && mainHand.IsPinched)
                    {
                        var distance = Vec3.Distance(offHand.pinchPt, mainHand.pinchPt);

                        if (_initialPinchDistance == 0)
                        {
                            _initialPinchDistance = distance;
                            _initialScale = ActivePainting.Scale;
                        }

                        _doNotPaintUntilPinchRelease = true;

                        var delta = (distance - _initialPinchDistance);

                        // Future: Try to scale around the center of the manipulation.
                        // Current attempts fail, as it seems like the Handle does not like my attempts

                        ActivePainting.Scale = _initialScale * (1 + delta * 2);
                        ActivePainting.PushPositionAndScaleToNetwork();
                    }
                    else
                    {
                        _initialPinchDistance = 0;
                    }

                    if (wasHandleInteractedWith)
                        _activePainting.PushPositionAndScaleToNetwork();

                    // Painting
                    using (HierarchyUtils.HierarchyItemScope(offsetMatrix))
                    {
                        var localHandPosGrid = AlignWithGrid(Hierarchy.ToLocal(mainHand.pinchPt));

                        foreach (var gridPos in _gridPositions)
                        {
                            var gridDrawPos = gridPos - Vec3.One * 0.5f;

                            if (gridDrawPos.InRadius(localHandPosGrid, 1f))
                            {
                                var color = Color.HSV(0.3f, 0.8f, 0.8f, 0.75f);
                                // _gridCubeModel.Draw(Matrix.TS(gridDrawPos, 0.05f), color);
                            }
                        }

                        if (
                            !wasHandleInteractedWith
                            && !PaletteWindowState.Frozen
                            && (!UI.IsInteracting(Handed.Right))
                            && !_doNotPaintUntilPinchRelease
                            && !(offHand.IsTracked && offHand.IsPinched)
                        )
                        {
                            if (mainHand.IsPinched)
                            {
                                var totalChangesBefore = Box3DModelData.TotalChanges;
                                switch (PaletteWindowState.SelectedTool)
                                {
                                    case PaintingTool.Paint:
                                        Box3DModelData.CreateOrUpdatePixel(
                                            localHandPosGrid,
                                            PaletteWindowState.SelectedColor(),
                                            _currentVoxelKind,
                                            GetDirection()
                                        );
                                        break;
                                    case PaintingTool.Delete:
                                        Box3DModelData.DeletePixel(localHandPosGrid);
                                        break;
                                    case PaintingTool.ColorPick:
                                        var pixelData = Box3DModelData.GetActivePixelAtPosition(
                                            localHandPosGrid
                                        );
                                        if (pixelData.HasValue)
                                            PaletteWindowState.SetHSVFromColor(
                                                pixelData.Value.Color
                                            );
                                        break;
                                    default:
                                        throw new ArgumentOutOfRangeException();
                                }

                                if (totalChangesBefore != Box3DModelData.TotalChanges)
                                {
                                    _cubeCreateSoundSequencer.PlayNextSoundInSequence(
                                        mainHand.pinchPt
                                    );
                                }
                            }
                            else if (
                                PaletteWindowState.SelectedTool == PaintingTool.Paint
                                && _handMenu?.Active == false
                            )
                            {
                                // Draw a preview of a cube
                                var colorCopy = PaletteWindowState.SelectedColor();
                                colorCopy.a = 0.6f;
                                BoxModelDrawer.DrawSingle(
                                    _currentVoxelKind,
                                    localHandPosGrid,
                                    GetDirection(),
                                    colorCopy
                                );
                            }
                        }

                        if (mainHand.IsJustUnpinched)
                        {
                            _cubeCreateSoundSequencer.ResetToFirst();
                        }

                        if (PaletteWindowState.Frozen)
                        {
                            DrawCubeModel(
                                Box3DModelData
                                    .CalculateVisiblePixelsAtPointInHistory(
                                        (int)PaletteWindowState.Time
                                    )
                                    .ToArray()
                            );
                        }
                        else
                        {
                            DrawCubeModel(
                                Box3DModelData.ActivePixelDatas,
                                localHandPosGrid,
                                PaletteWindowState.SelectedTool
                            );
                        }
                    }
                }
            }

            if (_doNotPaintUntilPinchRelease)
            {
                // Check to see if we have released the pinch since we started the last painting.
                _doNotPaintUntilPinchRelease = mainHand.IsPinched;
            }
        }

        private Quat GetDirection()
        {
            var hand = Input.Hand(Handed.Right);

            return snapToNearestRightAngle(Hierarchy.ToLocal(hand.palm.orientation));
        }

        //takes a rotation and returns the rotation that is the closest with all axes pointing at 90 degree intervals to the identity quaternion
        private Quat snapToNearestRightAngle(Quat currentRotation)
        {
            Vec3 closestToForward = closestToAxis(currentRotation, Vec3.Forward);
            Vec3 closestToUp = closestToAxis(currentRotation, Vec3.Up);

            return Quat.LookAt(Vec3.Zero, closestToForward * -1, closestToUp);
        }

        //finds the axis that is closest to the currentRotations local axis
        private Vec3 closestToAxis(Quat currentRotation, Vec3 axis)
        {
            Vec3[] checkAxes = new Vec3[]
            {
                Vec3.Forward,
                Vec3.Right,
                Vec3.Up,
                -Vec3.Forward,
                -Vec3.Right,
                -Vec3.Up
            };
            Vec3 closestToAxis = Vec3.Forward;
            float highestDot = -1;
            foreach (Vec3 checkAxis in checkAxes)
                check(ref highestDot, ref closestToAxis, currentRotation, axis, checkAxis);
            return closestToAxis;
        }

        //finds the closest axis to the input rotations specified axis
        private void check(
            ref float highestDot,
            ref Vec3 closest,
            Quat currentRotation,
            Vec3 axis,
            Vec3 checkDir
        )
        {
            float dot = Vec3.Dot(currentRotation * axis, checkDir);
            if (dot > highestDot)
            {
                highestDot = dot;
                closest = checkDir;
            }
        }

        private void DrawCubeModel(
            IReadOnlyList<Box3DModelData.PixelData> modelDataToDraw,
            Vec3 localHandGridPos = default,
            PaintingTool paintingTool = PaintingTool.None
        )
        {
            foreach (var cube in modelDataToDraw)
            {
                var cubeColor = cube.Color;
                if (localHandGridPos.v == cube.Position.v)
                {
                    var headLocalPos = Hierarchy.ToLocal(Input.Head.position);
                    switch (paintingTool)
                    {
                        case PaintingTool.None:
                            break;
                        case PaintingTool.Paint:
                            break;
                        case PaintingTool.Delete:
                            _xMarksTheSpot.Draw(
                                Matrix.TRS(
                                    cube.Position,
                                    Quat.LookAt(cube.Position, headLocalPos),
                                    1
                                )
                            );
                            cubeColor.a = 0.7f;
                            break;
                        case PaintingTool.ColorPick:
                            var hand = Input.Hand(Handed.Right);

                            var localHandPosGrid = AlignWithGrid(Hierarchy.ToLocal(hand.pinchPt));

                            var pixelData = Box3DModelData.GetActivePixelAtPosition(
                                localHandPosGrid
                            );
                            if (pixelData.HasValue)
                            {
                                Color colorPickerColor = pixelData.Value.Color;

                                _colorPickerInside.Draw(
                                    Matrix.TS(cube.Position, 0.4f),
                                    colorPickerColor
                                );

                                _colorPickerSphere.Draw(
                                    Matrix.TS(cube.Position, 0.4f),
                                    colorPickerColor
                                );
                            }

                            cubeColor.a = 0.5f;
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(
                                nameof(paintingTool),
                                paintingTool,
                                null
                            );
                    }
                }

                BoxModelDrawer.DrawSingle(cube, cubeColor);
            }
        }

        bool DrawToolButton(string id, Model model, float scae)
        {
            var scale = scae * U.cm;
            // Reserve a spot for this swatch!
            Bounds layoutBounds = UI.LayoutReserve(
                model.Bounds.dimensions.XY * scale,
                addPadding: false,
                depth: U.cm * 1
            );

            bool wasClickedThisFrame = false;
            layoutBounds.dimensions.z *= 2;
            BtnState state = UI.VolumeAt(id, layoutBounds, UIConfirm.Push);
            if (state.IsJustActive())
            {
                Sound.Click.Play(Hierarchy.ToWorld(layoutBounds.center));
                wasClickedThisFrame = true;
            }

            if (state.IsJustInactive())
                Sound.Unclick.Play(Hierarchy.ToWorld(layoutBounds.center));

            var depthModifier = state.IsActive() ? 0.5f : 1;
            var sizeModifier = state.IsActive() ? 1.1f : 1f;
            model.Draw(
                Matrix.TRS(
                    layoutBounds.center,
                    Quat.FromAngles(0, 0, 0),
                    new Vec3(scale * sizeModifier, scale * sizeModifier, scale * depthModifier)
                )
            );

            return wasClickedThisFrame;
        }

        bool SwatchButton(string id, float hue, float saturation, float value)
        {
            var scale = 4f * U.cm;
            // Reserve a spot for this swatch!
            Bounds layoutBounds = UI.LayoutReserve(
                _paletteColorModel.Bounds.dimensions.XY * scale,
                addPadding: false,
                depth: U.cm * 2
            );

            bool wasClickedThisFrame = false;

            layoutBounds.dimensions.z *= 2;
            BtnState state = UI.VolumeAt(id, layoutBounds, UIConfirm.Push);
            if (state.IsJustActive())
            {
                Sound.Click.Play(Hierarchy.ToWorld(layoutBounds.center));
                wasClickedThisFrame = true;
            }

            if (state.IsJustInactive())
                Sound.Unclick.Play(Hierarchy.ToWorld(layoutBounds.center));

            var isSelectedColor = Math.Abs(hue - PaletteWindowState.Hue) < 0.0001;

            var depthModifier = state.IsActive() || isSelectedColor ? 0.2f : 1;
            var sizeModifier = state.IsActive() ? 1.2f : 1f;

            _paletteColorModel.Draw(
                Matrix.TRS(
                    layoutBounds.center,
                    Quat.FromAngles(0, 0, 0),
                    new Vec3(
                        scale * sizeModifier,
                        scale * sizeModifier,
                        5 * layoutBounds.dimensions.z * depthModifier
                    )
                ),
                Color.HSV(hue, saturation, value)
            );

            return wasClickedThisFrame;
        }

        // ReSharper disable once CognitiveComplexity
        private void UpdatePaletteUI()
        {
            var menuBounds = _menuBoard.Bounds;
            menuBounds.dimensions.z *= 15; // Add interaction padding. !!! This assumes we do not have any draggable stuff in the UI!
            using (
                UIUtils.UIHandleScope(
                    "PaletteTemp",
                    ref _paletteWindowPose,
                    menuBounds,
                    false,
                    UIMove.Exact,
                    out var wasInteractedWithThisFrame
                )
            )
            {
                Renderer.Add(_menuBoard, Matrix.T(0, 0, 0));

                UI.LayoutArea(
                    new Vec3((_menuBoard.Bounds.dimensions.x / 2f), 0, 0) // If we switch the mesh to have anchor top left this should be removed.
                        + new Vec3(-16, 0, 0) * U.mm,
                    new Vec2(_menuBoard.Bounds.dimensions.x, 10f)
                );

                UI.ShowVolumes = false;
                if (ActivePainting == null)
                {
                    if (UI.Button("Open Model Manager"))
                    {
                        OpenModelManager!.Invoke();
                    }

                    return;
                }

                _colorPickerBoard.Draw(
                    Matrix.T(0, 0, -3 * U.mm),
                    PaletteWindowState.SelectedColor()
                );

                // This Palette UI with buttons for random colors etc is super temporary, and can and should be renewed.

                int column = 0;
                const int linebreakInterval = 5;
                for (float hue = 0; hue < 1; hue += 0.05f)
                {
                    if (
                        SwatchButton(
                            "col__" + hue,
                            hue,
                            PaletteWindowState.Saturation,
                            PaletteWindowState.Value
                        )
                    )
                    {
                        PaletteWindowState.Hue = hue;
                        PaletteWindowState.SelectedTool = PaintingTool.Paint;
                    }

                    column++;
                    if (column % linebreakInterval != 0)
                        UI.SameLine();
                    else
                    {
                        if (column == linebreakInterval * 4)
                        {
                            UI.SameLine();
                            UI.LayoutReserve(new Vec2(1 * U.mm, 0));
                            UI.SameLine();

                            if (
                                DrawToolButton(
                                    "Draw",
                                    PaletteWindowState.SelectedTool == PaintingTool.Paint
                                      ? _paintButtonPushed
                                      : _paintButtonNormal,
                                    3.7f
                                )
                            )
                            {
                                PaletteWindowState.SelectedTool = PaintingTool.Paint;
                            }
                            UI.SameLine();
                            if (
                                DrawToolButton(
                                    "Erase",
                                    PaletteWindowState.SelectedTool == PaintingTool.Delete
                                      ? _eraseButtonPushed
                                      : _eraseButtonNormal,
                                    3.7f
                                )
                            )
                            {
                                PaletteWindowState.SelectedTool = PaintingTool.Delete;
                            }
                            UI.SameLine();
                            if (
                                DrawToolButton(
                                    "ColorPicker",
                                    PaletteWindowState.SelectedTool == PaintingTool.ColorPick
                                      ? _colorPickerButtonPushed
                                      : _colorPickerButtonNormal,
                                    3.7f
                                )
                            )
                            {
                                PaletteWindowState.SelectedTool = PaintingTool.ColorPick;
                            }
                        }
                    }
                }

                UI.Text("");
                UI.SameLine();
                if (UI.HSlider("Saturation", ref PaletteWindowState.Saturation, 0f, 1f, 0.0001f))
                {
                    PaletteWindowState.SelectedTool = PaintingTool.Paint;
                }
                UI.SameLine();
                UI.Text("Saturation");

                UI.Text("");
                UI.SameLine();
                if (UI.HSlider("Value", ref PaletteWindowState.Value, 0f, 1f, 0.0001f))
                {
                    PaletteWindowState.SelectedTool = PaintingTool.Paint;
                }
                UI.SameLine();
                UI.Text("Brightness");

                var ctrlTab = // Dev helper.
                    Input.Key(Key.Ctrl).HasFlag(BtnState.Active)
                    && Input.Key(Key.Tab).HasFlag(BtnState.JustActive);

                if (ctrlTab)
                {
                    _currentVoxelKind += 1;
                    if (_currentVoxelKind > Box3DModelData.VoxelKind.Chipped)
                        _currentVoxelKind = Box3DModelData.VoxelKind.Cube;
                }

                var ctrlZ = // Dev helper.
                    Input.Key(Key.Ctrl).HasFlag(BtnState.Active)
                    && Input.Key(Key.Z).HasFlag(BtnState.JustActive);

                if (Box3DModelData.TotalChanges > 0 && (UI.Button("Undo Last") || ctrlZ))
                {
                    Box3DModelData.UndoLastEdit();
                }

                const string boxesFileExtension = ".boxes";
                const string gltfExtension = ".glb";
                if (DrawToolButton("Save", _saveButton, 5f))
                {
                    var fileBaseName = WordSequenceGenerator.GenerateRandomWordSequence(2);

                    var path = GetDefaultFolder(fileBaseName + boxesFileExtension);

                    if (!Platform.WriteFile(path, Box3DModelData.Serialize()))
                    {
                        Console.Error.WriteLine("Failed to write file to path " + path);
                    }

                    using var dataStream = new MemoryStream();
                    GltfExportBox3DModel.ExportModelAsGlbToStream(Box3DModelData, dataStream);
                    var time = DateTimeOffset.UtcNow.ToString("s").Replace(":", "_");
                    var outputFilePath = GetDefaultFolder($"{time}_model{gltfExtension}");
                    Console.WriteLine(outputFilePath);
                    if (!Platform.WriteFile(outputFilePath, dataStream.ToArray()))
                    {
                        Console.Error.WriteLine("Could not write to " + outputFilePath);
                    }
                }

                UI.SameLine();
                if (DrawToolButton("Load", _loadButton, 5f))
                {
                    OpenModelManager.Invoke();
                }

                UI.SameLine();
                UI.LayoutReserve(new Vec2(20.5f * U.cm, 0));
                UI.SameLine();

                var resetButtonPosition = UI.LayoutAt;
                if (
                    DrawToolButton(
                        "ResetAll",
                        _nukeButtons[PaletteWindowState.PressesUntilDeleteAll],
                        4f + PaletteWindowState.PressesUntilDeleteAll
                    )
                )
                {
                    PaletteWindowState.PressesUntilDeleteAll++;
                    if (PaletteWindowState.PressesUntilDeleteAll >= 3)
                    {
                        AssetLookup.Sounds.UI.Explode1.Play(Hierarchy.ToWorld(resetButtonPosition));
                        Box3DModelData.ClearChanges(Box3DModelData.TotalChanges);
                        PaletteWindowState.PressesUntilDeleteAll = 0;
                    }
                    PaletteWindowState.SelectedTool = PaintingTool.Paint;
                }

                // UI.Text("Scale");
                // UI.HSlider("Scale", ref ActivePainting.Scale, 0.02f, 0.2f, 0.0001f);

                var freezeButtonClicked = UI.Button(PaletteWindowState.Frozen ? "Edit" : "Freeze");

                if (freezeButtonClicked)
                {
                    PaletteWindowState.Frozen = !PaletteWindowState.Frozen;
                    if (PaletteWindowState.Frozen)
                        PaletteWindowState.Time = Box3DModelData.TotalChanges;
                }

                if (PaletteWindowState.Frozen)
                {
                    UI.Text("Time");
                    UI.HSlider(
                        "Time",
                        ref PaletteWindowState.Time,
                        0,
                        Box3DModelData.TotalChanges,
                        1
                    );
                }

                // UI.HSeparator();
                if (PaletteWindowState.Frozen)
                {
                    if (UI.Button("Edit from here (There is no way back!)"))
                    {
                        Box3DModelData.ClearChanges(
                            Box3DModelData.TotalChanges - (int)PaletteWindowState.Time
                        );
                        PaletteWindowState.Frozen = false;
                    }
                }
                // UI.HSeparator();

                // if (UI.Button("Open Model Manager"))
                // {
                //     OpenModelManager.Invoke();
                // }

                // const string boxesFileExtension = ".boxes";
                // if (UI.Button("Save to disk!"))
                // {
                //     var fileBaseName = WordSequenceGenerator.GenerateRandomWordSequence(2);
                //
                //     var path = GetDefaultFolder(fileBaseName + boxesFileExtension);
                //
                //     if (!Platform.WriteFile(path, Box3DModelData.Serialize()))
                //     {
                //         Console.Error.WriteLine("Failed to write file to path " + path);
                //     }
                //     //
                //     // Platform.FilePicker(
                //     //     PickerMode.Save,
                //     //     path =>
                //     //     {
                //     //         // For some reason the path given is without extension on Windows emulator. Not sure if its a issue in StereoKit?
                //     //         if (Path.GetExtension(path) == "")
                //     //         {
                //     //             path += boxesFileExtension;
                //     //         }
                //     //
                //     //         if (!Platform.WriteFile(path, _box3DModelData.Serialize()))
                //     //         {
                //     //             Console.Error.WriteLine("Failed to write file to path " + path);
                //     //         }
                //     //     },
                //     //     onCancel: () =>
                //     //     {
                //     //         Console.WriteLine("File saver canceled.");
                //     //     },
                //     //     boxesFileExtension
                //     // );
                // }
                //
                // UI.SameLine();
                // if (UI.Button("Load from disk"))
                // {
                //     Platform.FilePicker(
                //         PickerMode.Open,
                //         onSelectFile: (path) =>
                //         {
                //             var fileText = Platform.ReadFileText(path);
                //             if (fileText == null)
                //                 Console.WriteLine($"Failed to read file at path {path}");
                //             // else
                //             //     Box3DModelData = Box3DModelData.Deserialize(fileText);
                //         },
                //         onCancel: () =>
                //         {
                //             Console.WriteLine("File picker canceled.");
                //         },
                //         boxesFileExtension
                //     );
                // }
                //
                // const string gltfExtension = ".glb";
                // if (UI.Button($"Export as {gltfExtension}!"))
                // {
                //     using var dataStream = new MemoryStream();
                //     GltfExportBox3DModel.ExportModelAsGlbToStream(Box3DModelData, dataStream);
                //     var time = DateTimeOffset.UtcNow.ToString("s").Replace(":", "_");
                //     var outputFilePath = GetDefaultFolder($"{time}_model{gltfExtension}");
                //     Console.WriteLine(outputFilePath);
                //     if (!Platform.WriteFile(outputFilePath, dataStream.ToArray()))
                //     {
                //         Console.Error.WriteLine("Could not write to " + outputFilePath);
                //     }
                //     // Platform.FilePicker(
                //     //     PickerMode.Save,
                //     //     outputFilePath =>
                //     //     {
                //     //          // For some reason the path given is without extension on Windows emulator. Not sure if its a issue in StereoKit?
                //     //          if (Path.GetExtension(outputFilePath) == "")
                //     //         {
                //     //             outputFilePath += gltfExtension;
                //     //         }
                //     //
                //     //         using var dataStream = new MemoryStream();
                //     //         GltfExportBox3DModel.ExportModelAsGlbToStream(Box3DModelData, dataStream);
                //     //         Console.WriteLine(outputFilePath);
                //     //         if (!Platform.WriteFile(outputFilePath, dataStream.ToArray()))
                //     //         {
                //     //             Console.Error.WriteLine("Could not write to " + outputFilePath);
                //     //         }
                //     //     },
                //     //     onCancel: () =>
                //     //     {
                //     //         Console.WriteLine("File saver canceled.");
                //     //     },
                //     //     gltfExtension
                //     // );
                // }
            }
        }

        private string GetDefaultFolder(string fileName = "")
        {
            return Path.Combine(App.WorkDirectory.FullName, fileName);
        }

        private Vec3 AlignWithGrid(Vec3 pos)
        {
            var x = (float)Math.Round(pos.x);
            var y = (float)Math.Round(pos.y);
            var z = (float)Math.Round(pos.z);
            return new Vec3(x, y, z);
        }
    }
}
