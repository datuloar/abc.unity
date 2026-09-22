using System;
using System.Buffers.Binary;

using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public static partial class ServerSnapshotCodec
    {
        public static bool TryGetBaseline(ReadOnlySpan<byte> packet, out ActorNetworkId actorId, out ulong baselineTick)
        {
            actorId = default;
            baselineTick = 0;
            if (packet.Length < 5 || packet.Length > MaxPacketBytes || packet[0] != DeltaFormat)
                return false;
            var offset = 1;
            if (!SnapshotWire.TryReadUnsigned(packet, ref offset, out var id) || id == 0 ||
                !SnapshotWire.TryReadUnsigned(packet, ref offset, out var currentTick) ||
                !SnapshotWire.TryReadUnsigned(packet, ref offset, out var requiredTick) ||
                requiredTick >= currentTick || offset >= packet.Length || packet[offset] > 127)
                return false;
            actorId = new ActorNetworkId(id);
            baselineTick = requiredTick;
            return true;
        }

        public static bool TryRead(ReadOnlySpan<byte> packet, in QuantizedServerSnapshot baseline,
            out QuantizedServerSnapshot snapshot)
        {
            snapshot = default;
            if (packet.Length < 4 || packet.Length > MaxPacketBytes ||
                (packet[0] != FullFormat && packet[0] != DeltaFormat))
                return false;
            var offset = 1;
            if (!SnapshotWire.TryReadUnsigned(packet, ref offset, out var id) || id == 0 ||
                !SnapshotWire.TryReadUnsigned(packet, ref offset, out var tick))
                return false;

            var decoded = packet[0] == FullFormat
                ? TryReadFull(packet, ref offset, id, tick, out var candidate)
                : TryReadDelta(packet, ref offset, id, tick, in baseline, out candidate);
            if (!decoded || offset != packet.Length)
                return false;
            snapshot = candidate;
            return true;
        }

        private static bool TryReadFull(ReadOnlySpan<byte> packet, ref int offset, ulong id, ulong tick,
            out QuantizedServerSnapshot snapshot)
        {
            snapshot = default;
            if (!SnapshotWire.TryReadUnsigned(packet, ref offset, out var input) || packet.Length - offset != FullStateBytes)
                return false;
            var position = new Vector3Int();
            var velocity = new Vector3Int();
            for (var axis = 0; axis < 3; axis++)
            {
                position[axis] = BinaryPrimitives.ReadInt32LittleEndian(packet.Slice(offset));
                offset += sizeof(int);
                if (position[axis] < -QuantizedServerSnapshot.MaxPositionUnits ||
                    position[axis] > QuantizedServerSnapshot.MaxPositionUnits)
                    return false;
            }
            for (var axis = 0; axis < 3; axis++)
            {
                velocity[axis] = BinaryPrimitives.ReadInt16LittleEndian(packet.Slice(offset));
                offset += sizeof(short);
                if (velocity[axis] < -QuantizedServerSnapshot.MaxVelocityUnits)
                    return false;
            }
            snapshot = new QuantizedServerSnapshot(new ActorNetworkId(id), tick, input, position, velocity);
            return true;
        }

        private static bool TryReadDelta(ReadOnlySpan<byte> packet, ref int offset, ulong id, ulong tick,
            in QuantizedServerSnapshot baseline, out QuantizedServerSnapshot snapshot)
        {
            snapshot = default;
            if (!baseline.IsValid || baseline.ActorId.Value != id || tick <= baseline.Tick ||
                !SnapshotWire.TryReadUnsigned(packet, ref offset, out var baselineTick) ||
                baselineTick != baseline.Tick || offset >= packet.Length)
                return false;
            var mask = packet[offset++];
            if (mask > 127 || !TryReadInput(packet, ref offset, mask, baseline.LastInput, out var input))
                return false;
            var position = baseline.Position;
            var velocity = baseline.Velocity;
            for (var axis = 0; axis < 3; axis++)
            {
                if (!TryReadAxis(packet, ref offset, mask, PositionMask << axis, position[axis],
                    QuantizedServerSnapshot.MaxPositionUnits, out var positionValue) ||
                    !TryReadAxis(packet, ref offset, mask, VelocityMask << axis, velocity[axis],
                        QuantizedServerSnapshot.MaxVelocityUnits, out var velocityValue))
                    return false;
                position[axis] = positionValue;
                velocity[axis] = velocityValue;
            }
            snapshot = new QuantizedServerSnapshot(new ActorNetworkId(id), tick, input, position, velocity);
            return true;
        }

        private static bool TryReadInput(ReadOnlySpan<byte> packet, ref int offset, byte mask,
            ulong baseline, out ulong input)
        {
            input = baseline;
            if ((mask & LastInputMask) == 0)
                return true;
            if (!SnapshotWire.TryReadUnsigned(packet, ref offset, out var difference) ||
                difference > ulong.MaxValue - baseline)
                return false;
            input += difference;
            return true;
        }

        private static bool TryReadAxis(ReadOnlySpan<byte> packet, ref int offset, byte mask, int field,
            int baseline, int limit, out int value)
        {
            value = baseline;
            return (mask & field) == 0 || SnapshotWire.TryReadDifference(packet, ref offset, baseline, limit, out value);
        }
    }
}
