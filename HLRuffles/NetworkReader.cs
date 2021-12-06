using System;
using System.Text;
using StereoKit;
using StereoKitApp.HLRuffles.SyncStructs;

namespace StereoKitApp.HLRuffles
{
    public static class NetworkReader
    {
        private static int _offset;

        /// <summary>
        /// Read data from an ArraySegment into the given type.
        /// A subset of structs are supported for now. Please implement others as needed.
        /// </summary>
        /// <param name="data">The data array to read from</param>
        /// <param name="offset">Where does the data segment start</param>
        /// <typeparam name="T">Which type should we parse</typeparam>
        /// <returns>The parsed data</returns>
        /// <exception cref="NotImplementedException">If the type is not implemented yet</exception>
        public static T ReadData<T>(ArraySegment<byte> data, int offset) where T : struct
        {
            _offset = offset;
            var dataType = typeof(T);

            if (dataType == typeof(int))
                return (T)(object)ReadInt32Packet(data);
            if (dataType == typeof(float))
                return (T)(object)ReadFloatPacket(data);
            if (dataType == typeof(bool))
                return (T)(object)ReadBoolPacket(data);
            if (dataType == typeof(Vec3))
                return (T)(object)ReadVec3Packet(data);
            if (dataType == typeof(Color))
                return (T)(object)ReadColorPacket(data);
            if (dataType == typeof(Color32))
                return (T)(object)ReadColor32Packet(data);
            if (dataType == typeof(Quat))
                return (T)(object)ReadQuatPacket(data);
            if (dataType == typeof(Pose))
                return (T)(object)ReadPosePacket(data);

            if (dataType == typeof(HandStruct))
                return (T)(object)ReadHandPacket(data);
            if (dataType == typeof(AvatarSpawnStruct))
                return (T)(object)ReadAvatarPacket(data);
            if (dataType == typeof(PainterStruct))
                return (T)(object)ReadPainterPacket(data);
            if (dataType == typeof(PaintingSpawnerStruct))
                return (T)(object)ReadPaintingSpawnerPacket(data);

            throw new NotImplementedException();
        }

        private static byte ReadBytePacket(ArraySegment<byte> data)
        {
            var readData = data.Array![_offset];
            _offset += 1;
            return readData;
        }

        private static char ReadCharPacket(ArraySegment<byte> data)
        {
            char readData = (char)data.Array![_offset];
            _offset += 1;
            return readData;
        }

        private static bool ReadBoolPacket(ArraySegment<byte> data)
        {
            var readData = (data.Array![_offset] & 0x03) == 0;
            _offset += 1;
            return readData;
        }

        private static int ReadInt32Packet(ArraySegment<byte> data)
        {
            var readData = BitConverter.ToInt32(data.Array!, _offset);
            _offset += 4;
            return readData;
        }

        private static float ReadFloatPacket(ArraySegment<byte> data)
        {
            var readData = BitConverter.ToSingle(data.Array!, _offset);
            _offset += 4;
            return readData;
        }

        private static string ReadStringPacket(ArraySegment<byte> data)
        {
            int stringLength = ReadInt32Packet(data);
            var builder = new StringBuilder(stringLength + 1);
            for (int i = 0; i < stringLength; i++)
                builder.Insert(i, ReadCharPacket(data));
            _offset += stringLength;
            return builder.ToString();
        }

        private static Vec3 ReadVec3Packet(ArraySegment<byte> data) =>
            new Vec3(ReadFloatPacket(data), ReadFloatPacket(data), ReadFloatPacket(data));

        private static Color ReadColorPacket(ArraySegment<byte> data) =>
            new Color(
                ReadFloatPacket(data),
                ReadFloatPacket(data),
                ReadFloatPacket(data),
                ReadFloatPacket(data)
            );

        private static Color32 ReadColor32Packet(ArraySegment<byte> data) =>
            new Color32(
                ReadBytePacket(data),
                ReadBytePacket(data),
                ReadBytePacket(data),
                ReadBytePacket(data)
            );

        private static Quat ReadQuatPacket(ArraySegment<byte> data) =>
            new Quat(
                ReadFloatPacket(data),
                ReadFloatPacket(data),
                ReadFloatPacket(data),
                ReadFloatPacket(data)
            );

        private static Pose ReadPosePacket(ArraySegment<byte> data) =>
            new Pose(ReadVec3Packet(data), ReadQuatPacket(data));

        private static HandStruct ReadHandPacket(ArraySegment<byte> data) =>
            new HandStruct
            {
                ThumbTip = ReadVec3Packet(data),
                IndexTip = ReadVec3Packet(data),
                MiddleTip = ReadVec3Packet(data),
                RingTip = ReadVec3Packet(data),
                LittleTip = ReadVec3Packet(data),
                ThumbMiddle = ReadVec3Packet(data),
                IndexMiddle = ReadVec3Packet(data),
                MiddleMiddle = ReadVec3Packet(data),
                RingMiddle = ReadVec3Packet(data),
                LittleMiddle = ReadVec3Packet(data),
                ThumbProximal = ReadVec3Packet(data),
                IndexProximal = ReadVec3Packet(data),
                MiddleProximal = ReadVec3Packet(data),
                RingProximal = ReadVec3Packet(data),
                LittleProximal = ReadVec3Packet(data)
            };

        private static AvatarSpawnStruct ReadAvatarPacket(ArraySegment<byte> data) =>
            new AvatarSpawnStruct
            {
                AvatarNetworkId = ReadInt32Packet(data),
                IsOwner = ReadInt32Packet(data)
            };

        private static PainterStruct ReadPainterPacket(ArraySegment<byte> data) =>
            new PainterStruct
            {
                VoxelPos = ReadVec3Packet(data),
                VoxelColor = ReadColorPacket(data),
                PaintingAction = ReadBytePacket(data),
                VoxelKind = ReadBytePacket(data),
                CubeRotation = ReadQuatPacket(data)
            };

        private static PaintingSpawnerStruct ReadPaintingSpawnerPacket(ArraySegment<byte> data) =>
            new PaintingSpawnerStruct
            {
                PaintingSyncID = ReadInt32Packet(data),
                PaintingData = ReadStringPacket(data)
            };
    }
}
