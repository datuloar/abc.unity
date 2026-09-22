using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    [Serializable]
    public sealed class ArenaHealthData : IActorData
    {
        [SerializeField, Min(1f)] private float _maximum = 100f;
        [SerializeField, Min(0f)] private float _current = 100f;

        public float Current => _current;
        public float Maximum => _maximum;
        public float Normalized => _maximum > 0f ? _current / _maximum : 0f;
        public bool IsAlive => _current > 0f;

        public void Configure(float maximum)
        {
            _maximum = Mathf.Max(1f, maximum);
            _current = _maximum;
        }

        public bool ApplyDamage(float amount)
        {
            if (amount <= 0f || _current <= 0f)
                return false;

            _current = Mathf.Max(0f, _current - amount);
            return _current <= 0f;
        }
    }
}
