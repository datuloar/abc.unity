using System;

namespace Abc.Unity.Samples.NetworkSimulation
{
    internal static class SnapshotWire
    {
        internal static int GetUnsignedSize(ulong value)
        {
            var size = 1;
            while (value >= 128)
            {
                value >>= 7;
                size++;
            }
            return size;
        }

        internal static uint EncodeSigned(int value) => unchecked((uint)((value << 1) ^ (value >> 31)));

        internal static void WriteUnsigned(Span<byte> destination, ref int offset, ulong value)
        {
            while (value >= 128)
            {
                destination[offset++] = (byte)((value & 127) | 128);
                value >>= 7;
            }
            destination[offset++] = (byte)value;
        }

        internal static bool TryReadUnsigned(ReadOnlySpan<byte> source, ref int offset, out ulong value)
        {
            value = 0;
            for (var shift = 0; shift <= 63; shift += 7)
            {
                if (offset >= source.Length)
                    return false;
                var current = source[offset++];
                if (shift == 63 && current > 1)
                    return false;
                value |= (ulong)(current & 127) << shift;
                if ((current & 128) == 0)
                    return shift == 0 || current != 0;
            }
            return false;
        }

        internal static bool TryReadDifference(ReadOnlySpan<byte> source, ref int offset,
            int baseline, int limit, out int value)
        {
            value = 0;
            if (!TryReadUnsigned(source, ref offset, out var encoded) || encoded > uint.MaxValue)
                return false;
            var difference = (int)(encoded >> 1) ^ -(int)(encoded & 1);
            var sum = (long)baseline + difference;
            if (sum < -limit || sum > limit)
                return false;
            value = (int)sum;
            return true;
        }
    }
}
