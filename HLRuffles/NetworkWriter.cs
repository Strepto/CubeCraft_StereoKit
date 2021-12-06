using System;
using System.Linq;
using StereoKit;
using StereoKitApp.HLRuffles.SyncStructs;

namespace StereoKitApp.HLRuffles
{
    public static class NetworkWriter
    {
        private static byte[] _dataBuffer = new byte[1024 * 1024];
        private static int _offset;

        public static ArraySegment<byte> WriteData<T>(T data, int id) where T : struct
        {
            _offset = 0;

            var type = typeof(T);
            WriteIntPacket(id);

            if (type == typeof(int))
                WriteIntPacket((int)(object)data);
            if (type == typeof(float))
                WriteFloatPacket((float)(object)data);
            if (type == typeof(bool))
                WriteBoolPacket((bool)(object)data);
            if (type == typeof(Vec3))
                WriteVec3Packet((Vec3)(object)data);
            if (type == typeof(Color))
                WriteColorPacket((Color)(object)data);
            if (type == typeof(Color32))
                WriteColor32Packet((Color32)(object)data);
            if (type == typeof(Quat))
                WriteQuatPacket((Quat)(object)data);
            if (type == typeof(Pose))
                WritePosePacket((Pose)(object)data);

            if (type == typeof(HandStruct))
                WriteHandacket((HandStruct)(object)data);
            if (type == typeof(AvatarSpawnStruct))
                WriteAvatarSpawnPacket((AvatarSpawnStruct)(object)data);
            if (type == typeof(PainterStruct))
                WritePainterPacket((PainterStruct)(object)data);
            if (type == typeof(PaintingSpawnerStruct))
                WritePaintingSpawnerPacket((PaintingSpawnerStruct)(object)data);

            return new ArraySegment<byte>(_dataBuffer, 0, _offset);
        }

        private static void WriteBytePacket(byte data)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(data), 0, _dataBuffer, _offset, sizeof(byte));
            _offset += 1;
        }

        private static void WriteIntPacket(int data)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(data), 0, _dataBuffer, _offset, sizeof(int));
            _offset += 4;
        }

        private static void WriteBoolPacket(bool data)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(data), 0, _dataBuffer, _offset, sizeof(byte));
            _offset += 1;
        }

        private static void WriteFloatPacket(float data)
        {
            Buffer.BlockCopy(BitConverter.GetBytes(data), 0, _dataBuffer, _offset, sizeof(float));
            _offset += 4;
        }

        private static void WriteStringPacket(string data)
        {
            WriteIntPacket(data.Length);
            Buffer.BlockCopy(
                data.Select(Convert.ToByte).ToArray(),
                0,
                _dataBuffer,
                _offset,
                data.Length
            );
            _offset += data.Length;
        }

        private static void WriteVec3Packet(Vec3 data)
        {
            WriteFloatPacket(data.x);
            WriteFloatPacket(data.y);
            WriteFloatPacket(data.z);
        }

        private static void WriteColorPacket(Color data)
        {
            WriteFloatPacket(data.r);
            WriteFloatPacket(data.g);
            WriteFloatPacket(data.b);
            WriteFloatPacket(data.a);
        }

        private static void WriteColor32Packet(Color32 data)
        {
            WriteBytePacket(data.r);
            WriteBytePacket(data.g);
            WriteBytePacket(data.b);
            WriteBytePacket(data.a);
        }

        private static void WriteQuatPacket(Quat data)
        {
            WriteFloatPacket(data.x);
            WriteFloatPacket(data.y);
            WriteFloatPacket(data.z);
            WriteFloatPacket(data.w);
        }

        private static void WritePosePacket(Pose data)
        {
            WriteVec3Packet(data.position);
            WriteQuatPacket(data.orientation);
        }

        private static void WriteHandacket(HandStruct data)
        {
            WriteVec3Packet(data.ThumbTip);
            WriteVec3Packet(data.IndexTip);
            WriteVec3Packet(data.MiddleTip);
            WriteVec3Packet(data.RingTip);
            WriteVec3Packet(data.LittleTip);

            WriteVec3Packet(data.ThumbMiddle);
            WriteVec3Packet(data.IndexMiddle);
            WriteVec3Packet(data.MiddleMiddle);
            WriteVec3Packet(data.RingMiddle);
            WriteVec3Packet(data.LittleMiddle);

            WriteVec3Packet(data.ThumbProximal);
            WriteVec3Packet(data.IndexProximal);
            WriteVec3Packet(data.MiddleProximal);
            WriteVec3Packet(data.RingProximal);
            WriteVec3Packet(data.LittleProximal);
        }

        private static void WriteAvatarSpawnPacket(AvatarSpawnStruct data)
        {
            WriteIntPacket(data.AvatarNetworkId);
            WriteIntPacket(data.IsOwner);
        }

        private static void WritePainterPacket(PainterStruct data)
        {
            WriteVec3Packet(data.VoxelPos);
            WriteColorPacket(data.VoxelColor);
            WriteBytePacket(data.PaintingAction);
            WriteBytePacket(data.VoxelKind);
            WriteQuatPacket(data.CubeRotation);
        }

        private static void WritePaintingSpawnerPacket(PaintingSpawnerStruct data)
        {
            WriteIntPacket(data.PaintingSyncID);
            WriteStringPacket(data.PaintingData);
        }
    }
}
