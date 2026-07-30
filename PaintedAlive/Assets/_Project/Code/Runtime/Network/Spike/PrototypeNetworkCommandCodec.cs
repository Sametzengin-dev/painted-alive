using System;
using System.IO;

namespace PaintedAlive.Network.Spike
{
    public static class PrototypeNetworkCommandCodec
    {
        private const byte SchemaVersion = 1;

        public static byte[] Encode(PrototypeNetworkStrokeCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            int pointCount = command.controlPoints?.Length ?? 0;
            if (pointCount > byte.MaxValue)
            {
                throw new InvalidOperationException("Stroke command supports at most 255 points.");
            }

            using var stream = new MemoryStream(40 + pointCount * 6);
            using var writer = new BinaryWriter(stream);

            writer.Write(SchemaVersion);
            writer.Write(command.strokeId);
            writer.Write(command.painterId);
            writer.Write(command.surfaceId);
            writer.Write((byte)command.materialType);
            writer.Write(command.startTick);
            writer.Write(command.seed);
            writer.Write(command.width);
            writer.Write(command.pressure);
            writer.Write(command.pigmentCost);
            writer.Write((byte)pointCount);

            for (int i = 0; i < pointCount; i++)
            {
                PrototypeQuantizedStrokePoint point = command.controlPoints[i];
                writer.Write(point.x);
                writer.Write(point.y);
                writer.Write(point.z);
            }

            writer.Flush();
            return stream.ToArray();
        }

        public static PrototypeNetworkStrokeCommand Decode(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                throw new ArgumentException("Packet is empty.", nameof(bytes));
            }

            using var stream = new MemoryStream(bytes, false);
            using var reader = new BinaryReader(stream);

            byte version = reader.ReadByte();
            if (version != SchemaVersion)
            {
                throw new InvalidDataException($"Unsupported stroke schema version {version}.");
            }

            var command = new PrototypeNetworkStrokeCommand
            {
                strokeId = reader.ReadInt32(),
                painterId = reader.ReadInt32(),
                surfaceId = reader.ReadInt32(),
                materialType = reader.ReadByte(),
                startTick = reader.ReadInt32(),
                seed = reader.ReadInt32(),
                width = reader.ReadUInt16(),
                pressure = reader.ReadUInt16(),
                pigmentCost = reader.ReadUInt16()
            };

            int pointCount = reader.ReadByte();
            command.controlPoints = new PrototypeQuantizedStrokePoint[pointCount];

            for (int i = 0; i < pointCount; i++)
            {
                command.controlPoints[i] = new PrototypeQuantizedStrokePoint
                {
                    x = reader.ReadInt16(),
                    y = reader.ReadInt16(),
                    z = reader.ReadInt16()
                };
            }

            if (stream.Position != stream.Length)
            {
                throw new InvalidDataException("Stroke packet contains trailing bytes.");
            }

            return command;
        }

        public static string ComputeFnv1A64Hex(byte[] bytes, ulong initial = 14695981039346656037UL)
        {
            ulong hash = initial;
            if (bytes != null)
            {
                for (int i = 0; i < bytes.Length; i++)
                {
                    hash ^= bytes[i];
                    hash *= 1099511628211UL;
                }
            }

            return hash.ToString("X16");
        }

        public static ulong AppendFnv1A64(ulong hash, byte[] bytes)
        {
            if (bytes == null)
            {
                return hash;
            }

            for (int i = 0; i < bytes.Length; i++)
            {
                hash ^= bytes[i];
                hash *= 1099511628211UL;
            }

            return hash;
        }
    }
}
