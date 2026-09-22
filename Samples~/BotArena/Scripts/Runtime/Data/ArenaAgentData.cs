using System;

using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    [Serializable]
    public sealed class ArenaAgentData : IActorData
    {
        [SerializeField] private bool _isPlayer;
        [SerializeField, Min(0.1f)] private float _moveSpeed = 4.5f;
        [SerializeField, Min(0.1f)] private float _radius = 0.6f;

        public bool IsPlayer => _isPlayer;
        public float MoveSpeed => _moveSpeed;
        public float Radius => _radius;

        public void Configure(bool isPlayer, float moveSpeed, float radius)
        {
            _isPlayer = isPlayer;
            _moveSpeed = Mathf.Max(0.1f, moveSpeed);
            _radius = Mathf.Max(0.1f, radius);
        }
    }
}
