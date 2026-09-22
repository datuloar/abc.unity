using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    internal sealed class ServerBodyData : IActorData
    {
        public ServerBodyData(ActorNetworkId id, ulong peerId, Rigidbody body)
        {
            Id = id;
            PeerId = peerId;
            Body = body;
        }

        public ActorNetworkId Id { get; }
        public ulong PeerId { get; }
        public Rigidbody Body { get; }
        public ulong LastSequence { get; set; }
        public ulong LastReceivedTick { get; set; }
        public ulong LastAppliedSequence { get; set; }
        public Vector2 Movement { get; set; }
        public int RemainingInputSteps { get; set; }

        public Vector3 Velocity
        {
#if UNITY_6000_0_OR_NEWER
            get => Body.linearVelocity;
            set => Body.linearVelocity = value;
#else
            get => Body.velocity;
            set => Body.velocity = value;
#endif
        }

        public void CleanUp()
        {
            if (Body == null)
                return;
            Body.gameObject.SetActive(false);
            if (Application.isPlaying)
                Object.Destroy(Body.gameObject);
            else
                Object.DestroyImmediate(Body.gameObject);
        }
    }
}
