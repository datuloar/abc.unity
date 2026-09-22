using UnityEngine;

using Abc.Unity;

namespace Abc.Unity.Samples.BotArena
{
    internal readonly struct ArenaFireCommand : IActorCommand
    {
        public ArenaFireCommand(Vector3 direction) => Direction = direction;

        public Vector3 Direction { get; }
    }
}
