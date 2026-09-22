using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal sealed class ArenaPositionData : IActorData
    {
        public ArenaPositionData(Vector3 value)
        {
            Value = value;
            PreviousValue = value;
        }

        public Vector3 Value;
        public Vector3 PreviousValue;
    }
}
