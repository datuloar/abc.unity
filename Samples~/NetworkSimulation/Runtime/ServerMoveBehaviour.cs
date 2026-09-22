using UnityEngine;

namespace Abc.Unity.Samples.NetworkSimulation
{
    internal sealed class ServerMoveBehaviour : IActorBehaviour, IActorFixedTick
    {
        private const float Speed = 3f;
        private ServerBodyData _data;

        public IActor Owner { get; set; }

        public void Initialize() => _data = Owner.GetData<ServerBodyData>();

        public void FixedTick(float fixedDeltaTime)
        {
            var movement = _data.RemainingInputSteps > 0 ? _data.Movement : Vector2.zero;
            if (_data.RemainingInputSteps > 0)
                _data.RemainingInputSteps--;

            _data.Velocity = new Vector3(movement.x * Speed, _data.Velocity.y, movement.y * Speed);
            _data.LastAppliedSequence = _data.LastSequence;
        }
    }
}
