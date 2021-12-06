using System.Collections.Generic;
using StereoKit;
using StereoKitApp.HLRuffles;
using StereoKitApp.HLRuffles.SyncStructs;

namespace StereoKitApp.Features.Avatar
{
    public class AvatarSpawner
    {
        private const int AvatarNetworkStartId = 100000;
        private SyncVar<AvatarSpawnStruct> _peerSpawned = new SyncVar<AvatarSpawnStruct>(
            AvatarNetworkStartId,
            new AvatarSpawnStruct()
        );
        private SyncVar<int> _peerRemoved = new SyncVar<int>(AvatarNetworkStartId + 1, 0);
        private Dictionary<int, Avatar> _avatars = new Dictionary<int, Avatar>();

        public AvatarSpawner()
        {
            RufflesTransport.Singleton.OnPeerConnected += OnPeerConnected;
            RufflesTransport.Singleton.OnPeerDisconnected += OnPeerDisconnected;
            _peerSpawned.ValueReceived += OnAvatarSpawned;
            _peerRemoved.ValueReceived += OnAvatarDespawned;
        }

        private void OnPeerConnected(ulong peerId)
        {
            if (!RufflesTransport.Singleton.IsHost)
                return;

            SpawnAllAvatarsToConnected(peerId);

            AvatarSpawnStruct spawnData = new AvatarSpawnStruct
            {
                AvatarNetworkId = AvatarNetworkStartId + (int)peerId * 3 + 5,
                IsOwner = 0
            };

            OnAvatarSpawned(spawnData);
            RufflesTransport.Singleton.SendDataWithExcludedId(
                NetworkWriter.WriteData(spawnData, AvatarNetworkStartId),
                peerId
            );

            spawnData.IsOwner = 1;
            RufflesTransport.Singleton.SendDataToId(
                NetworkWriter.WriteData(spawnData, AvatarNetworkStartId),
                peerId
            );
        }

        private void OnPeerDisconnected(ulong peerId)
        {
            if (RufflesTransport.Singleton.IsHost)
            {
                _peerRemoved.Value = AvatarNetworkStartId + (int)peerId * 3 + 5;
                OnAvatarDespawned(_peerRemoved.Value);
            }
            else
            {
                List<int> avatarIds = new List<int>();
                foreach (var keys in _avatars.Keys)
                {
                    avatarIds.Add(keys);
                }

                foreach (int avatarId in avatarIds)
                {
                    OnAvatarDespawned(avatarId);
                }
            }
        }

        private void SpawnAllAvatarsToConnected(ulong peerId)
        {
            if (!_avatars.ContainsKey(AvatarNetworkStartId + 2))
                CreateHostAvatar();

            foreach (var networkValues in _avatars.Keys)
            {
                AvatarSpawnStruct spawnData = new AvatarSpawnStruct
                {
                    AvatarNetworkId = networkValues,
                    IsOwner = 0
                };

                RufflesTransport.Singleton.SendDataToId(
                    NetworkWriter.WriteData(spawnData, AvatarNetworkStartId),
                    peerId
                );
            }
        }

        private void CreateHostAvatar()
        {
            AvatarSpawnStruct spawnData = new AvatarSpawnStruct
            {
                AvatarNetworkId = AvatarNetworkStartId + 2,
                IsOwner = 1
            };
            OnAvatarSpawned(spawnData);
        }

        private void OnAvatarSpawned(AvatarSpawnStruct spawnData)
        {
            var avatar = new Avatar(spawnData.AvatarNetworkId, spawnData.IsOwner > 0);
            _avatars.Add(spawnData.AvatarNetworkId, avatar);
            SK.AddStepper(avatar);
        }

        private void OnAvatarDespawned(int avtarId)
        {
            _avatars.TryGetValue(avtarId, out var avatar);

            if (avatar == null)
                return;

            avatar.Shutdown();
            SK.RemoveStepper(avatar);
            _avatars.Remove(avtarId);
        }

        ~AvatarSpawner()
        {
            RufflesTransport.Singleton.OnPeerConnected -= OnPeerConnected;
            RufflesTransport.Singleton.OnPeerDisconnected -= OnPeerDisconnected;
            _peerSpawned.ValueReceived -= OnAvatarSpawned;
            _peerRemoved.ValueReceived -= OnAvatarDespawned;
        }
    }
}
