using System;
using System.Collections.Generic;
using StereoKitApp.HLRuffles;
using StereoKitApp.HLRuffles.SyncStructs;

namespace StereoKitApp.Painting.Sharing
{
    public class PaintingSpawner
    {
        public Action<PaintingSpawnerStruct> OnPaintingSpawned = null!;

        private const int NumberOfSyncedIds = 3;

        private int _nextPaintingId = SyncIds.PaintPlacemenSyncer;
        private SyncVar<PaintingSpawnerStruct> _paintingSpawnData =
            new SyncVar<PaintingSpawnerStruct>(SyncIds.PaintSpawner);
        private HashSet<PaintingManager.PaintingInstance> _paintingInstances;

        public PaintingSpawner(HashSet<PaintingManager.PaintingInstance> paintingInstances)
        {
            _paintingInstances = paintingInstances;
            _paintingSpawnData.ValueReceived += PaintingSpawned;

            RufflesTransport.Singleton.OnPeerConnected += OnPeerConnected;
        }

        private void OnPeerConnected(ulong peerId)
        {
            if (!RufflesTransport.Singleton.IsHost)
                return;

            foreach (PaintingManager.PaintingInstance spawnedId in _paintingInstances)
            {
                PaintingSpawnerStruct spawnData = new PaintingSpawnerStruct
                {
                    PaintingSyncID = spawnedId.PaintingSyncer.NetworkId,
                    PaintingData = spawnedId.Box3DModelData.Serialize()
                };

                RufflesTransport.Singleton.SendDataToId(
                    NetworkWriter.WriteData(spawnData, SyncIds.PaintSpawner),
                    peerId
                );
            }
        }

        private void PaintingSpawned(PaintingSpawnerStruct paintingSpawnData)
        {
            OnPaintingSpawned.Invoke(paintingSpawnData);
            if (paintingSpawnData.PaintingSyncID >= _nextPaintingId)
                _nextPaintingId = paintingSpawnData.PaintingSyncID + NumberOfSyncedIds;
        }

        public PaintingSyncer SpawnPainting(string jsonData)
        {
            _paintingSpawnData.Value = new PaintingSpawnerStruct
            {
                PaintingSyncID = _nextPaintingId,
                PaintingData = jsonData
            };

            PaintingSyncer paintingSyncer = new PaintingSyncer(_nextPaintingId);
            _nextPaintingId += NumberOfSyncedIds;

            return paintingSyncer;
        }

        public void ResetSpawner()
        {
            _paintingInstances.Clear();
            _nextPaintingId = SyncIds.PaintPlacemenSyncer;
        }
    }
}
