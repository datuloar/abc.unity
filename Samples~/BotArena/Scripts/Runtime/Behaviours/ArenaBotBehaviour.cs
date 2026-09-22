using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaBotBehaviour : IActorBehaviour, IActorTick
    {
        private readonly ArenaSession _session;
        private ArenaAgentData _agent;
        private ArenaBotData _bot;
        private ArenaPositionData _position;
        private ArenaVelocityData _velocity;
        private ArenaVisualData _visual;

        public ArenaBotBehaviour(ArenaSession session) => _session = session;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            _agent = Owner.GetData<ArenaAgentData>();
            _bot = Owner.GetData<ArenaBotData>();
            _position = Owner.GetData<ArenaPositionData>();
            _velocity = Owner.GetData<ArenaVelocityData>();
            _visual = Owner.GetData<ArenaVisualData>();
        }

        public void Tick(float deltaTime)
        {
            if (!_session.TryGetPlayerPosition(out var playerPosition))
            {
                _velocity.Value = Vector3.zero;
                return;
            }

            var toPlayer = playerPosition - _position.Value;
            toPlayer.y = 0f;
            var distance = toPlayer.magnitude;
            if (distance < 0.001f)
                return;

            var radial = toPlayer / distance;
            var tangent = Vector3.Cross(Vector3.up, radial) * _bot.OrbitDirection;
            var rangeForce = distance > _bot.PreferredRange ? radial : -radial * 0.55f;
            var steering = rangeForce + tangent * 0.72f;
            _velocity.Value = steering.normalized * _agent.MoveSpeed;
            _visual.Heading = radial;

            if (distance <= _bot.ShootRange)
                Owner.SendCommand(new ArenaFireCommand(radial));
        }
    }
}
