using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public readonly struct ServerSnapshot
    {
        public ServerSnapshot(ActorNetworkId actorId, ulong tick, ulong lastInput, Vector3 position, Vector3 velocity)
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
        public Vector3 Position { get; }
        public Vector3 Velocity { get; }
    }
}
