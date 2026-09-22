using System.Runtime.CompilerServices;

namespace Abc.Unity
{
    internal static class MathHelpers
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int NextPowerOf2(int value)
        {
            if (value < 0)
                throw new System.ArgumentOutOfRangeException(nameof(value));

            if (value > 1 << 30)
                throw new System.OverflowException();

            uint v = (uint)value;

            if (v == 0)
                return 0;

            v--;
            v |= v >> 1;
            v |= v >> 2;
            v |= v >> 4;
            v |= v >> 8;
            v |= v >> 16;
            v++;

            return (int)v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsPowerOfTwo(int value) => value > 0 && unchecked(value & (value - 1)) == 0;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastLog2(int value)
        {
            if (value < 0)
                throw new System.ArgumentOutOfRangeException(nameof(value));

            return sizeof(int) * 8 - LeadingZeroesCount(value) - 1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastMod(int value, int mod)
        {
            if (value < 0)
                throw new System.ArgumentOutOfRangeException(nameof(value));

            if (!IsPowerOfTwo(mod))
                throw new System.ArgumentOutOfRangeException(nameof(mod));

            return unchecked(value & (mod - 1));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FastPowDiv(int value, int powerOfTwo)
        {
            if (value < 0)
                throw new System.ArgumentOutOfRangeException(nameof(value));

            if ((uint)powerOfTwo > 30u)
                throw new System.ArgumentOutOfRangeException(nameof(powerOfTwo));

            return value >> powerOfTwo;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int LeadingZeroesCount(int x)
        {
            x |= x >> 1;
            x |= x >> 2;
            x |= x >> 4;
            x |= x >> 8;
            x |= x >> 16;

            x -= x >> 1 & 0x55555555;
            x = (x >> 2 & 0x33333333) + (x & 0x33333333);
            x = (x >> 4) + x & 0x0f0f0f0f;
            x += x >> 8;
            x += x >> 16;

            return sizeof(int) * 8 - (x & 0x0000003f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int CombineHashes(int a, int b)
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + a;
                hash = hash * 31 + b;
                return hash;
            }
        }
    }
}
