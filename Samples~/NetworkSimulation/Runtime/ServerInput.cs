using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    public readonly struct ServerInput
    {
        public ServerInput(ActorNetworkId actorId, ulong sequence, Vector2 movement)
        {
            ActorId = actorId;
            Sequence = sequence;
            Movement = movement;
        }

        public ActorNetworkId ActorId { get; }
        public ulong Sequence { get; }
        public Vector2 Movement { get; }
    }
}
