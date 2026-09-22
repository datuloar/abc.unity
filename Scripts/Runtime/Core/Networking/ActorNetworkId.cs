using System;
using System.Globalization;

namespace Abc.Unity
{
    public readonly struct ActorNetworkId : IEquatable<ActorNetworkId>
    {
        public ActorNetworkId(ulong value)
        {
            if (value == 0)
                throw new ArgumentOutOfRangeException(nameof(value), "Network ID zero is reserved for an invalid ID.");
            Value = value;
        }

        public ulong Value { get; }
        public bool IsValid => Value != 0;

        public bool Equals(ActorNetworkId other) => Value == other.Value;
        public override bool Equals(object obj) => obj is ActorNetworkId other && Equals(other);
        public override int GetHashCode() => Value.GetHashCode();
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);

        public static bool operator ==(ActorNetworkId left, ActorNetworkId right) => left.Equals(right);
        public static bool operator !=(ActorNetworkId left, ActorNetworkId right) => !left.Equals(right);
    }
}
