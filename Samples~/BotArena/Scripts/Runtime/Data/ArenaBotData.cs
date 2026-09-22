using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    [Serializable]
    public sealed class ArenaBotData : IActorData
    {
        [SerializeField, Min(1f)] private float _preferredRange = 7f;
        [SerializeField, Min(1f)] private float _shootRange = 13f;
        [SerializeField] private float _orbitDirection = 1f;

        public float PreferredRange => _preferredRange;
        public float ShootRange => _shootRange;
        public float OrbitDirection => _orbitDirection;

        public void Configure(float preferredRange, float shootRange, float orbitDirection)
        {
            _preferredRange = Mathf.Max(1f, preferredRange);
            _shootRange = Mathf.Max(_preferredRange, shootRange);
            _orbitDirection = orbitDirection < 0f ? -1f : 1f;
        }
    }
}
