using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaVelocityData : IActorData
    {
        public ArenaVelocityData(Vector3 value) => Value = value;

        public Vector3 Value;
    }
}
