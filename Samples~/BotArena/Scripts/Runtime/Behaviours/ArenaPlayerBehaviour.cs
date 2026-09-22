using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaPlayerBehaviour : IActorBehaviour, IActorTick
    {
        private readonly ArenaSession _session;
        private ArenaAgentData _agent;
        private ArenaPositionData _position;
        private ArenaVelocityData _velocity;
        private ArenaVisualData _visual;

        public ArenaPlayerBehaviour(ArenaSession session) => _session = session;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            _agent = Owner.GetData<ArenaAgentData>();
            _position = Owner.GetData<ArenaPositionData>();
            _velocity = Owner.GetData<ArenaVelocityData>();
            _visual = Owner.GetData<ArenaVisualData>();
        }

        public void Tick(float deltaTime)
        {
            if (_session.IsGameOver)
            {
                _velocity.Value = Vector3.zero;
                return;
            }

            var movement = Vector3.zero;
            var fire = false;

#if ENABLE_LEGACY_INPUT_MANAGER
            movement.x = Input.GetAxisRaw("Horizontal");
            movement.z = Input.GetAxisRaw("Vertical");
            fire = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
#endif

            if (movement.sqrMagnitude > 1f)
                movement.Normalize();

            _velocity.Value = movement * _agent.MoveSpeed;
            var aim = _session.GetAimDirection(_position.Value);
            if (aim.sqrMagnitude > 0.001f)
                _visual.Heading = aim;

            if (fire)
                Owner.SendCommand(new ArenaFireCommand(_visual.Heading));
        }
    }
}
