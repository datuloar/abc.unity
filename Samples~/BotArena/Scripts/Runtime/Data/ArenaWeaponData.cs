using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    [Serializable]
    public sealed class ArenaWeaponData : IActorData
    {
        [SerializeField, Min(0.03f)] private float _fireInterval = 0.8f;
        [SerializeField, Min(1f)] private float _damage = 12f;
        [SerializeField, Min(1f)] private float _projectileSpeed = 12f;

        private float _cooldown;

        public float Damage => _damage;
        public float FireInterval => _fireInterval;
        public float ProjectileSpeed => _projectileSpeed;
        public bool CanFire => _cooldown <= 0f;

        public void Configure(float fireInterval, float damage, float projectileSpeed)
        {
            _fireInterval = Mathf.Max(0.03f, fireInterval);
            _damage = Mathf.Max(1f, damage);
            _projectileSpeed = Mathf.Max(1f, projectileSpeed);
            _cooldown = 0f;
        }

        public void Tick(float deltaTime)
        {
            if (_cooldown > 0f)
                _cooldown -= deltaTime;
        }

        public void ConsumeShot() => _cooldown = _fireInterval;

        public void Delay(float duration) => _cooldown = Mathf.Max(_cooldown, duration);
    }
}
