using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.Basic
{
    public sealed class ActorWorldExample : MonoBehaviour
    {
        private sealed class SimulationPosition : IActorData
        {
            public Vector3 Value;
        }

        private sealed class SimulationVelocity : IActorData
        {
            public Vector3 Value;
        }

        private struct MoveAction : IActorQueryAction<SimulationPosition, SimulationVelocity>
        {
            public float DeltaTime;

            public void Execute(ActorModel actor, SimulationPosition position, SimulationVelocity velocity)
            {
                position.Value += velocity.Value * DeltaTime;
            }
        }

        [SerializeField, Min(1)] private int _actorCount = 1000;
        [SerializeField] private Vector3 _velocity = Vector3.forward;

        private ActorWorld _world;
        private ActorWorldQuery<SimulationPosition, SimulationVelocity> _query;
        private MoveAction _move;

        private void Awake()
        {
            _world = new ActorWorld("Sample Simulation", _actorCount);

            for (var i = 0; i < _actorCount; i++)
            {
                _world.Add(new ActorModel($"Model {i}")
                    .WithData(new SimulationPosition())
                    .WithData(new SimulationVelocity { Value = _velocity }));
            }

            _query = _world.Query<SimulationPosition, SimulationVelocity>();
        }

        private void Update()
        {
            _move.DeltaTime = Time.deltaTime;
            _query.For(ref _move);
        }

        private void OnDestroy() => _world?.Dispose();
    }
}
