using System;
using System.Buffers.Binary;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public static partial class ServerSnapshotCodec
    {
        public const int MaxPacketBytes = 49;
        private const byte FullFormat = 0x10;
        private const byte DeltaFormat = 0x11;
        private const int FullStateBytes = 3 * sizeof(int) + 3 * sizeof(short);
        private const byte LastInputMask = 1;
        private const byte PositionMask = 2;
        private const byte VelocityMask = 16;

        public static bool TryWrite(in QuantizedServerSnapshot snapshot,
            in QuantizedServerSnapshot acknowledgedBaseline, Span<byte> destination, out int bytesWritten)
        {
            bytesWritten = 0;
            if (!snapshot.IsValid)
                return false;
            var mask = GetChangeMask(in snapshot, in acknowledgedBaseline);
            var size = GetFullSize(in snapshot);
            var deltaSize = CanUseBaseline(in snapshot, in acknowledgedBaseline)
                ? GetDeltaSize(in snapshot, in acknowledgedBaseline, mask)
                : int.MaxValue;
            var isDelta = deltaSize < size;
            if (isDelta)
                size = deltaSize;
            if (destination.Length < size)
                return false;

            var offset = 0;
            destination[offset++] = isDelta ? DeltaFormat : FullFormat;
            SnapshotWire.WriteUnsigned(destination, ref offset, snapshot.ActorId.Value);
            SnapshotWire.WriteUnsigned(destination, ref offset, snapshot.Tick);
            if (isDelta)
                WriteDelta(destination, ref offset, in snapshot, in acknowledgedBaseline, mask);
            else
                WriteFull(destination, ref offset, in snapshot);
            bytesWritten = offset;
            return true;
        }

        private static bool CanUseBaseline(in QuantizedServerSnapshot snapshot,
            in QuantizedServerSnapshot baseline)
        {
            return baseline.IsValid && snapshot.ActorId == baseline.ActorId &&
                snapshot.Tick > baseline.Tick && snapshot.LastInput >= baseline.LastInput;
        }

        private static byte GetChangeMask(in QuantizedServerSnapshot snapshot,
            in QuantizedServerSnapshot baseline)
        {
            var mask = snapshot.LastInput != baseline.LastInput ? LastInputMask : (byte)0;
            for (var axis = 0; axis < 3; axis++)
            {
                if (snapshot.Position[axis] != baseline.Position[axis])
                    mask |= (byte)(PositionMask << axis);
                if (snapshot.Velocity[axis] != baseline.Velocity[axis])
                    mask |= (byte)(VelocityMask << axis);
            }
            return mask;
        }

        private static int GetFullSize(in QuantizedServerSnapshot snapshot)
        {
            return 1 + FullStateBytes + SnapshotWire.GetUnsignedSize(snapshot.ActorId.Value) +
                SnapshotWire.GetUnsignedSize(snapshot.Tick) + SnapshotWire.GetUnsignedSize(snapshot.LastInput);
        }

        private static int GetDeltaSize(in QuantizedServerSnapshot snapshot,
            in QuantizedServerSnapshot baseline, byte mask)
        {
            var size = 2 + SnapshotWire.GetUnsignedSize(snapshot.ActorId.Value) +
                SnapshotWire.GetUnsignedSize(snapshot.Tick) + SnapshotWire.GetUnsignedSize(baseline.Tick);
            if ((mask & LastInputMask) != 0)
                size += SnapshotWire.GetUnsignedSize(snapshot.LastInput - baseline.LastInput);
            for (var axis = 0; axis < 3; axis++)
            {
                if ((mask & (PositionMask << axis)) != 0)
                    size += SnapshotWire.GetUnsignedSize(SnapshotWire.EncodeSigned(
                        snapshot.Position[axis] - baseline.Position[axis]));
                if ((mask & (VelocityMask << axis)) != 0)
                    size += SnapshotWire.GetUnsignedSize(SnapshotWire.EncodeSigned(
                        snapshot.Velocity[axis] - baseline.Velocity[axis]));
            }
            return size;
        }

        private static void WriteFull(Span<byte> destination, ref int offset, in QuantizedServerSnapshot snapshot)
        {
            SnapshotWire.WriteUnsigned(destination, ref offset, snapshot.LastInput);
            for (var axis = 0; axis < 3; axis++)
            {
                BinaryPrimitives.WriteInt32LittleEndian(destination.Slice(offset), snapshot.Position[axis]);
                offset += sizeof(int);
            }
            for (var axis = 0; axis < 3; axis++)
            {
                BinaryPrimitives.WriteInt16LittleEndian(destination.Slice(offset), (short)snapshot.Velocity[axis]);
                offset += sizeof(short);
            }
        }

        private static void WriteDelta(Span<byte> destination, ref int offset,
            in QuantizedServerSnapshot snapshot, in QuantizedServerSnapshot baseline, byte mask)
        {
            SnapshotWire.WriteUnsigned(destination, ref offset, baseline.Tick);
            destination[offset++] = mask;
            if ((mask & LastInputMask) != 0)
                SnapshotWire.WriteUnsigned(destination, ref offset, snapshot.LastInput - baseline.LastInput);
            for (var axis = 0; axis < 3; axis++)
            {
                if ((mask & (PositionMask << axis)) != 0)
                    SnapshotWire.WriteUnsigned(destination, ref offset,
                        SnapshotWire.EncodeSigned(snapshot.Position[axis] - baseline.Position[axis]));
                if ((mask & (VelocityMask << axis)) != 0)
                    SnapshotWire.WriteUnsigned(destination, ref offset,
                        SnapshotWire.EncodeSigned(snapshot.Velocity[axis] - baseline.Velocity[axis]));
            }
        }
    }
}
