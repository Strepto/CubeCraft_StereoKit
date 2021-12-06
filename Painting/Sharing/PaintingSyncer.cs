using System;
using StereoKit;
using StereoKitApp.HLRuffles;
using StereoKitApp.HLRuffles.SyncStructs;
using StereoKitApp.Utils.EqualityComparers;

namespace StereoKitApp.Painting.Sharing
{
    public class PaintingSyncer : IDisposable
    {
        public Action<Box3DModelData.PixelData> OnVoxelEdited;
        public int NetworkId { get; }

        private SyncVar<PainterStruct> _voxelData;
        public SyncVar<Pose> Pose;
        public SyncVar<float> Scale;

        public PaintingSyncer(int networkId)
        {
            _voxelData = new SyncVar<PainterStruct>(networkId, null);
            Pose = new SyncVar<Pose>(networkId + 1, new Pose(), PoseEqualityComparer.Instance);
            Scale = new SyncVar<float>(networkId + 2, 1);
            _voxelData.ValueReceived += OnVoxelDataUpdated;
            NetworkId = networkId;
        }

        private void OnVoxelDataUpdated(PainterStruct newVoxelData)
        {
            OnVoxelEdited?.Invoke(
                new Box3DModelData.PixelData()
                {
                    Color = newVoxelData.VoxelColor,
                    Position = newVoxelData.VoxelPos,
                    PixelStatus = (Box3DModelData.PixelStatus) newVoxelData.PaintingAction,
                    VoxelKind = (Box3DModelData.VoxelKind) newVoxelData.VoxelKind,
                    CubeRotation = newVoxelData.CubeRotation
                }
            );
        }

        public void SendVoxelEdit(Box3DModelData.PixelData pixelData)
        {
            _voxelData.Value = new PainterStruct
            {
                VoxelPos = pixelData.Position,
                VoxelColor = pixelData.Color,
                PaintingAction = (byte) pixelData.PixelStatus,
                VoxelKind = (byte) pixelData.VoxelKind,
                CubeRotation = pixelData.CubeRotation
            };
        }

        public void Dispose()
        {
            _voxelData.Dispose();
            Pose.Dispose();
            Scale.Dispose();
        }
    }
}
