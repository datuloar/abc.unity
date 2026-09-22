using System;

using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public readonly struct QuantizedServerSnapshot
    {
        public const int UnitsPerMeter = 100;
        public const int MaxPositionUnits = 1000000;
        public const int MaxVelocityUnits = short.MaxValue;
        private const float MaxPosition = (float)MaxPositionUnits / UnitsPerMeter;
        private const float MaxVelocity = (float)MaxVelocityUnits / UnitsPerMeter;

        internal QuantizedServerSnapshot(ActorNetworkId actorId, ulong tick, ulong lastInput,
            Vector3Int position, Vector3Int velocity)
        {
            ActorId = actorId;
            Tick = tick;
            LastInput = lastInput;
            Position = position;
            Velocity = velocity;
        }

        public ActorNetworkId ActorId { get; }
        public ulong Tick { get; }
        public ulong LastInput { get; }
        public Vector3Int Position { get; }
        public Vector3Int Velocity { get; }
        public bool IsValid => ActorId.IsValid;

        public static bool TryCreate(in ServerSnapshot snapshot, out QuantizedServerSnapshot quantized)
        {
            quantized = default;
            if (!snapshot.ActorId.IsValid ||
                !TryQuantize(snapshot.Position, MaxPosition, out var position) ||
                !TryQuantize(snapshot.Velocity, MaxVelocity, out var velocity))
                return false;
            quantized = new QuantizedServerSnapshot(snapshot.ActorId, snapshot.Tick,
                snapshot.LastInput, position, velocity);
            return true;
        }

        public ServerSnapshot ToSnapshot()
        {
            return new ServerSnapshot(ActorId, Tick, LastInput,
                (Vector3)Position / UnitsPerMeter, (Vector3)Velocity / UnitsPerMeter);
        }

        private static bool TryQuantize(Vector3 value, float limit, out Vector3Int quantized)
        {
            quantized = default;
            if (!TryQuantize(value.x, limit, out var x) ||
                !TryQuantize(value.y, limit, out var y) ||
                !TryQuantize(value.z, limit, out var z))
                return false;
            quantized = new Vector3Int(x, y, z);
            return true;
        }

        private static bool TryQuantize(float value, float limit, out int quantized)
        {
            quantized = 0;
            if (float.IsNaN(value) || float.IsInfinity(value) ||
                value < -limit || value > limit)
                return false;
            quantized = (int)Math.Round((double)value * UnitsPerMeter, MidpointRounding.AwayFromZero);
            return true;
        }
    }
}
