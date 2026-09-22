using System;
using System.Runtime.CompilerServices;

using UnityEngine;

namespace Abc.Unity
{
    [Serializable]
    public struct ActorTag : IEquatable<ActorTag>
    {
        public static readonly ActorTag Default = new ActorTag(0);
        public static readonly ActorTag Player = new ActorTag(1);
        public static readonly ActorTag Enemy = new ActorTag(2);

        [SerializeField] private int _value;

        public ActorTag(int value) => _value = value;

        public int Value => _value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(ActorTag other) => _value == other._value;

        public override bool Equals(object obj) => obj is ActorTag other && Equals(other);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() => _value;

        public override string ToString()
        {
            switch (_value)
            {
                case 0:
                    return nameof(Default);
                case 1:
                    return nameof(Player);
                case 2:
                    return nameof(Enemy);
                default:
                    return _value.ToString();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ActorTag left, ActorTag right) => left.Equals(right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ActorTag left, ActorTag right) => !left.Equals(right);
    }
}
