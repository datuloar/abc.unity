using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaWeaponBehaviour : IActorBehaviour, IActorTick, IActorCommandListener<ArenaFireCommand>
    {
        private readonly ArenaSession _session;
        private ArenaPositionData _position;
        private ArenaWeaponData _weapon;

        public ArenaWeaponBehaviour(ArenaSession session) => _session = session;

        public IActor Owner { get; set; }

        public void Initialize()
        {
            _position = Owner.GetData<ArenaPositionData>();
            _weapon = Owner.GetData<ArenaWeaponData>();
        }

        public void Tick(float deltaTime) => _weapon.Tick(deltaTime);

        public void ReactActorCommand(ArenaFireCommand command)
        {
            if (!_weapon.CanFire || command.Direction.sqrMagnitude < 0.001f)
                return;

            var direction = command.Direction.normalized;
            _session.SpawnProjectile(Owner, _position.Value + direction * 0.85f, direction, _weapon);
            _weapon.ConsumeShot();
        }
    }
}
