using System;
using System.Collections.Generic;
using System.IO;
using StereoKit;
using StereoKitApp.HLRuffles;
using StereoKitApp.HLRuffles.SyncStructs;
using StereoKitApp.Painting.Sharing;
using StereoKitApp.Utils;
using StereoKitApp.Utils.WordSequenceGenerator;

namespace StereoKitApp.Painting
{
    public class PaintingManager
    {
        public class PaintingInstance : IDisposable
        {
            public float Scale;
            public Pose Pose;

            public string Name { get; }
            public Box3DModelData Box3DModelData { get; }
            public PaintingSyncer PaintingSyncer { get; }

            public PaintingInstance(
                string name,
                Box3DModelData box3DModelData,
                PaintingSyncer paintingSyncer,
                Vec3 position,
                float scale
            )
            {
                Pose = new Pose(position, Quat.Identity);
                Scale = scale;
                Name = name;
                Box3DModelData = box3DModelData;
                PaintingSyncer = paintingSyncer;

                box3DModelData.Box3DModelDataEdited += Box3DModelDataEdited;
                paintingSyncer.OnVoxelEdited += OnVoxelCreated;

                paintingSyncer.Scale.ValueReceived += newScale => Scale = newScale;
                paintingSyncer.Pose.ValueReceived += pose => Pose = pose;
            }

            public void PushPositionAndScaleToNetwork()
            {
                PaintingSyncer.Scale.Value = Scale;
                PaintingSyncer.Pose.Value = Pose;
            }

            private void OnVoxelCreated(Box3DModelData.PixelData pixelData)
            {
                Box3DModelData.AddPixelEdit(pixelData, true);
            }

            private void Box3DModelDataEdited(Box3DModelData.PixelEdit pixelEdit)
            {
                PaintingSyncer.SendVoxelEdit(pixelEdit.PixelData);
            }

            public void Dispose()
            {
                PaintingSyncer.Dispose();
            }
        }

        private readonly PaintingSpawner _paintingSpawner;

        private readonly HashSet<PaintingInstance> _paintingInstances =
            new HashSet<PaintingInstance>();

        private GridPainter _gridPainter = null!;
        private PaintingInstance? _activePainting => _gridPainter.ActivePainting;
        private readonly BoxModelManagementUi _boxModelManagementUi;

        private (DateTimeOffset, PaintingInstance) _lastInteractedNonActivePainting;
        private float _initialPinchDistance;
        private float _initialScale;

        public PaintingManager(DirectoryInfo workDirectory)
        {
            _paintingSpawner = new PaintingSpawner(_paintingInstances);
            _paintingSpawner.OnPaintingSpawned += OnPaintingSpawned;
            _gridPainter = new GridPainter(16);
            _boxModelManagementUi = new BoxModelManagementUi(workDirectory);

            _gridPainter.OpenModelManager += () =>
            {
                _boxModelManagementUi.SetActive(true);
            };

            _boxModelManagementUi.InitializeModelAtPosAndScale += InitializeModelAtPosAndScale;
            RufflesTransport.Singleton.OnStartingSession += OnStartingSession;
        }

        private void OnStartingSession()
        {
            if (RufflesTransport.Singleton.IsHost)
                return;

            foreach (PaintingInstance paintingInstance in _paintingInstances)
            {
                paintingInstance.Dispose();
            }

            _paintingInstances.Clear();
            _paintingSpawner.ResetSpawner();
        }

        private void InitializeModelAtPosAndScale(
            Vec3 worldPosition,
            float scale,
            Box3DModelData modelToInitialize
        )
        {
            InstatiatePainting(worldPosition, scale, modelToInitialize);
        }

        public void Update()
        {
            if (_activePainting == null)
                return;

            _gridPainter.Update();
            _boxModelManagementUi.Update();

            foreach (var paintingInstance in _paintingInstances)
            {
                // Assuming GridPainter handles the active painting.
                if (paintingInstance == _activePainting)
                    continue;
                var scaledBounds = paintingInstance.Box3DModelData.ActivePixelDatasBounds.Scaled(
                    paintingInstance.Scale
                );

                var mainHand = Input.Hand(Handed.Right);
                var offHand = Input.Hand(Handed.Left);

                using (
                    UIUtils.UIHandleScope(
                        paintingInstance.Name + "handle",
                        ref paintingInstance.Pose,
                        scaledBounds,
                        false,
                        UIMove.Exact,
                        out bool wasInteractedWithThisFrame
                    )
                )
                {
                    if (wasInteractedWithThisFrame)
                    {
                        _lastInteractedNonActivePainting = (
                            DateTimeOffset.UtcNow,
                            paintingInstance
                        );

                        if (mainHand.IsPinched && offHand.IsPinched)
                        {
                            var distance = Vec3.Distance(offHand.pinchPt, mainHand.pinchPt);

                            if (_initialPinchDistance == 0)
                            {
                                _initialPinchDistance = distance;
                                _initialScale = paintingInstance.Scale;
                            }

                            var delta = (distance - _initialPinchDistance);

                            // Future: Try to scale around the center of the manipulation.
                            // Current attempts fail, as it seems like the Handle does not like my attempts

                            paintingInstance.Scale = _initialScale * (1 + delta * 2);
                        }

                        paintingInstance.PushPositionAndScaleToNetwork();
                    }
                    else
                    {
                        _initialPinchDistance = default;
                        _initialScale = 1;
                    }

                    BoxModelDrawer.Draw(
                        paintingInstance.Box3DModelData,
                        Vec3.Zero,
                        paintingInstance.Scale
                    );
                }

                if (
                    _lastInteractedNonActivePainting != default
                    && DateTimeOffset.Now - _lastInteractedNonActivePainting.Item1
                        < TimeSpan.FromSeconds(6)
                    && _lastInteractedNonActivePainting.Item2 == paintingInstance
                )
                {
                    var uiPos = paintingInstance.Pose.ToMatrix().Transform(scaledBounds.center);

                    uiPos.y -= scaledBounds.dimensions.y * 0.6f;
                    var tempPose = new Pose(uiPos, Quat.LookAt(uiPos, Input.Head.position));
                    using (
                        UIUtils.UIWindowScope(
                            paintingInstance.Name + "_wnd",
                            ref tempPose,
                            Vec2.Zero,
                            UIWin.Body,
                            UIMove.None
                        )
                    )
                    {
                        if (UI.Button("Edit"))
                        {
                            _gridPainter.ActivePainting = paintingInstance;
                        }

                        if (UI.Button("Remove (Discards any changes)"))
                        {
                            paintingInstance.Dispose();
                            _paintingInstances.Remove(paintingInstance);
                        }
                    }
                }
            }
        }

        public void InstatiatePainting(
            Vec3 pos,
            float initialScaleOverride,
            Box3DModelData modelToInitialize
        )
        {
            var paintingInstance = new PaintingInstance(
                WordSequenceGenerator.GenerateRandomWordSequence(2),
                modelToInitialize,
                _paintingSpawner.SpawnPainting(modelToInitialize.Serialize()),
                pos,
                initialScaleOverride
            );

            _paintingInstances.Add(paintingInstance);
            // TODO: Consider making this drawing active?
        }

        public void InstantiatePainting(string name)
        {
            var paintingInstance = new PaintingInstance(
                name,
                new Box3DModelData(),
                _paintingSpawner.SpawnPainting(""),
                Vec3.Zero,
                0.05f
            );
            _paintingInstances.Add(paintingInstance);

            _gridPainter.ActivePainting = paintingInstance;
        }

        private void OnPaintingSpawned(PaintingSpawnerStruct paintingSpawnerStruct)
        {
            var paintingInstance = new PaintingInstance(
                WordSequenceGenerator.GenerateRandomWordSequence(2), // TODO: Use real model name.
                Box3DModelData.Deserialize(paintingSpawnerStruct.PaintingData),
                new PaintingSyncer(paintingSpawnerStruct.PaintingSyncID),
                Vec3.Zero,
                0.01f
            );

            _paintingInstances.Add(paintingInstance);

            if (_activePainting == null)
            {
                _gridPainter.ActivePainting = paintingInstance;
            }
        }
    }
}
