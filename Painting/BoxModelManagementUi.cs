using System;
using System.IO;
using System.Linq;
using StereoKit;
using StereoKitApp.Utils;
using StereoKitApp.Utils.Easing;

namespace StereoKitApp.Painting
{
    public class BoxModelManagementUi
    {
        public Action<Vec3, float, Box3DModelData> InitializeModelAtPosAndScale = null!; // Must be listened to
        private readonly DirectoryInfo _directoryToBrowse;
        private (string Name, Box3DModelData)[] _box3DDatas;

        public BoxModelManagementUi(DirectoryInfo directoryToBrowse, bool startActive = false)
        {
            _directoryToBrowse = directoryToBrowse;
            _box3DDatas = ReadBoxFilesInDirectory(directoryToBrowse);
            SetActive(startActive);
        }

        private (string Name, Box3DModelData)[] ReadBoxFilesInDirectory(DirectoryInfo directory)
        {
            // TODO: We should be able to make this smarter..
            if (!directory.Exists)
            {
                return Array.Empty<(string name, Box3DModelData)>();
            }

            var boxFiles = directory.GetFiles("*.boxes");
            var box3DDatas = boxFiles
                .Select(
                    boxFile =>
                        (
                            boxFile.Name,
                            Box3DModelData.Deserialize(Platform.ReadFileText(boxFile.FullName))
                        )
                )
                .ToArray();

            return box3DDatas;
        }

        private float _baseRotation = 180;
        private readonly float _rotationInertia = 0.95f;
        private float _rotationSpeed = 0;
        private Vec3 _handPosLastFrame = Vec3.Zero;
        private Vec3 _handPinchStartPoint;
        private float _pinchStartRotation;

        private int _index = 0;

        private Vec3 _startPosition = Input.Head.position;
        private bool _active = false;
        private float _pinchElevation;

        public void SetActive(bool active, Vec3? startPosOverride = null)
        {
            if (active)
            {
                _startPosition = startPosOverride ?? (Input.Head.position - Vec3.Up * 0.3f);
                _rotationSpeed = 0;
                _baseRotation = 0;
                _box3DDatas = ReadBoxFilesInDirectory(_directoryToBrowse);
            }

            _active = active;
        }

        public void Update()
        {
            if (!_active)
                return;

            Hierarchy.Push(Matrix.T(_startPosition));

            Vec3 offset = Vec3.Zero;

            var hand = Input.Hand(Handed.Right);

            var itemsToShow = Math.Min(_box3DDatas.Length, 12); // Takes at most this amount of items.
            var offsetRot = (int)Math.Floor(_baseRotation / 20f) - (itemsToShow / 2);

            var i = 0;

            foreach (var (name, box3DModelData) in _box3DDatas.TakeLoop(-offsetRot, itemsToShow))
            {
                const float radius = 0.5f;
                var rotationDegrees = (i - offsetRot) * 20 + _baseRotation;

                var zDiff = SKMath.Sin(MathUtils.DegreesToRadians(rotationDegrees)) * radius;
                var xDiff = SKMath.Cos(MathUtils.DegreesToRadians(rotationDegrees)) * radius;

                offset.z = zDiff;
                offset.x = xDiff;

                var bounds = box3DModelData.ActivePixelDatasBounds;

                var largestXyDimension = Math.Max(bounds.dimensions.x, bounds.dimensions.y);

                const float realWorldMaxSizeInXyDimension = 0.1f;

                var scale = realWorldMaxSizeInXyDimension / largestXyDimension;

                var boundsScaled = bounds.dimensions * scale;
                var minProximityRange = boundsScaled.Magnitude / 2;
                float handProximityEased = (
                    Ease.QuartOut(
                        1
                            - Math.Min(minProximityRange, Vec3.Distance(offset, hand.pinchPt))
                                / minProximityRange
                    )
                );

                var tempPose = new Pose(offset, Quat.Identity);

                using (
                    UIUtils.UIHandleScope(
                        "handle-" + name,
                        ref tempPose,
                        new Bounds(Vec3.Zero, boundsScaled),
                        false,
                        UIMove.PosOnly,
                        out var wasInteractedWithThisFrame
                    )
                )
                {
                    var pinchScale = wasInteractedWithThisFrame ? 1 + _pinchElevation * 5 : 1;
                    var proximityScale = scale * (1 + 0.2f * handProximityEased) * pinchScale;
                    Vec3 worldPos;
                    using (HierarchyUtils.HierarchyItemScope(Matrix.S(proximityScale)))
                    {
                        var center = -bounds.center;
                        using (HierarchyUtils.HierarchyItemScope(Matrix.T(center)))
                        {
                            BoxModelDrawer.Draw(box3DModelData, Vec3.Zero, Vec3.One);
                            worldPos = Hierarchy.ToWorld(Vec3.Zero);
                        }
                    }

                    if (wasInteractedWithThisFrame)
                    {
                        if (hand.IsJustPinched)
                        {
                            _handPinchStartPoint = hand.pinchPt;
                            _pinchStartRotation = _baseRotation;
                            _pinchElevation = 0;
                        }

                        var toCameraSpaceTransformMatrix =
                            Matrix.TR(Input.Head.position, Input.Head.orientation).Inverse;

                        var currentCamSpaceHandPos = toCameraSpaceTransformMatrix.Transform(
                            hand.pinchPt
                        );
                        var prevFrameCamSpaceHandPos = toCameraSpaceTransformMatrix.Transform(
                            _handPosLastFrame
                        );
                        var handPinchStartPos = toCameraSpaceTransformMatrix.Transform(
                            _handPinchStartPoint
                        );

                        var frameDeltaHandPos = currentCamSpaceHandPos - prevFrameCamSpaceHandPos;
                        var initialPinchPosDelta = currentCamSpaceHandPos - handPinchStartPos;

                        _baseRotation = _pinchStartRotation + initialPinchPosDelta.x * 100;
                        _rotationSpeed += frameDeltaHandPos.x * 10;

                        _pinchElevation = initialPinchPosDelta.y;

                        if (initialPinchPosDelta.y >= 0.3f)
                        {
                            AssetLookup.Sounds.UI.Pop1.Play(hand.pinchPt);
                            InitialiseModelAndClose(worldPos, proximityScale, box3DModelData);
                        }
                    }
                }

                var offsetCopy = offset;
                offsetCopy.y -= 0.05f;
                var pose = new Pose(offsetCopy, Quat.LookAt(offsetCopy, Vec3.Zero));
                using (
                    UIUtils.UIWindowScope("wnd_" + i, ref pose, Vec2.Zero, UIWin.Empty, UIMove.None)
                )
                {
                    // if (UI.Button("Delete\n" + i + Path.GetFileNameWithoutExtension(name))) { }
                }

                i++;
            }

            if (_rotationSpeed != 0)
            {
                _rotationSpeed = (_rotationSpeed * _rotationInertia);
                _baseRotation += _rotationSpeed;
            }

            _handPosLastFrame = hand.pinchPt;
            // if (
            //     DateTimeOffset.UtcNow - _lastUpdateFromDiskUtc > TimeSpan.FromSeconds(1)
            //     && SK.ActiveDisplayMode == DisplayMode.Flatscreen
            // )
            // {
            //     _box3DDatas = ReadBoxFilesInDirectory(_directoryToBrowse);
            // }
            Hierarchy.Pop();
        }

        private void InitialiseModelAndClose(
            Vec3 worldPosition,
            float proximityScale,
            Box3DModelData box3DModelData
        )
        {
            InitializeModelAtPosAndScale.Invoke(worldPosition, proximityScale, box3DModelData);
            SetActive(false);
        }
    }
}
