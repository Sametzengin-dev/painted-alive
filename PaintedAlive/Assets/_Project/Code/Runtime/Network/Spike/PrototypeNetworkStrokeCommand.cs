using System;
using UnityEngine;

namespace PaintedAlive.Network.Spike
{
    [Serializable]
    public struct PrototypeQuantizedStrokePoint
    {
        public short x;
        public short y;
        public short z;
    }

    [Serializable]
    public sealed class PrototypeNetworkStrokeCommand
    {
        public int strokeId;
        public int painterId;
        public int surfaceId;
        public int materialType;
        public int startTick;
        public int seed;
        public ushort width;
        public ushort pressure;
        public ushort pigmentCost;
        public PrototypeQuantizedStrokePoint[] controlPoints;

        public static PrototypeNetworkStrokeCommand CreateDeterministic(
            int index,
            int pointCount,
            int seed,
            float positionStep,
            float maximumExtent)
        {
            var random = new System.Random(seed + index * 7919);
            var command = new PrototypeNetworkStrokeCommand
            {
                strokeId = index + 1,
                painterId = index % 4,
                surfaceId = 100 + index % 7,
                materialType = index % 4,
                startTick = 120 + index * 3,
                seed = seed ^ (index * 48611),
                width = QuantizeUnit(0.15f + (float)random.NextDouble() * 0.85f),
                pressure = QuantizeUnit((float)random.NextDouble()),
                pigmentCost = QuantizeUnit(0.10f + (float)random.NextDouble() * 0.65f),
                controlPoints = new PrototypeQuantizedStrokePoint[pointCount]
            };

            float baseX = Mathf.Lerp(-maximumExtent * 0.75f, maximumExtent * 0.75f,
                (float)random.NextDouble());
            float baseZ = Mathf.Lerp(-maximumExtent * 0.75f, maximumExtent * 0.75f,
                (float)random.NextDouble());

            for (int i = 0; i < pointCount; i++)
            {
                float t = pointCount <= 1 ? 0f : i / (float)(pointCount - 1);
                float x = baseX + t * 3.5f + Mathf.Sin(t * Mathf.PI * 2f + index) * 0.8f;
                float y = Mathf.Sin(t * Mathf.PI) * 0.35f;
                float z = baseZ + Mathf.Cos(t * Mathf.PI * 1.5f + index * 0.25f) * 1.2f;

                command.controlPoints[i] = new PrototypeQuantizedStrokePoint
                {
                    x = QuantizePosition(x, positionStep),
                    y = QuantizePosition(y, positionStep),
                    z = QuantizePosition(z, positionStep)
                };
            }

            return command;
        }

        public bool ContentEquals(PrototypeNetworkStrokeCommand other)
        {
            if (other == null ||
                strokeId != other.strokeId ||
                painterId != other.painterId ||
                surfaceId != other.surfaceId ||
                materialType != other.materialType ||
                startTick != other.startTick ||
                seed != other.seed ||
                width != other.width ||
                pressure != other.pressure ||
                pigmentCost != other.pigmentCost)
            {
                return false;
            }

            if (controlPoints == null || other.controlPoints == null ||
                controlPoints.Length != other.controlPoints.Length)
            {
                return controlPoints == null && other.controlPoints == null;
            }

            for (int i = 0; i < controlPoints.Length; i++)
            {
                if (controlPoints[i].x != other.controlPoints[i].x ||
                    controlPoints[i].y != other.controlPoints[i].y ||
                    controlPoints[i].z != other.controlPoints[i].z)
                {
                    return false;
                }
            }

            return true;
        }

        private static short QuantizePosition(float value, float step)
        {
            int quantized = Mathf.RoundToInt(value / Mathf.Max(0.001f, step));
            return (short)Mathf.Clamp(quantized, short.MinValue, short.MaxValue);
        }

        private static ushort QuantizeUnit(float value)
        {
            return (ushort)Mathf.RoundToInt(Mathf.Clamp01(value) * ushort.MaxValue);
        }
    }
}
