#if UNITY_EDITOR
using System;
using System.Collections.Generic;

namespace Abc.Unity
{
    internal static class ActorWorldDiagnostics
    {
        private static readonly List<WeakReference<ActorWorld>> Worlds = new List<WeakReference<ActorWorld>>();

        public static void Register(ActorWorld world)
        {
            Prune();
            Worlds.Add(new WeakReference<ActorWorld>(world));
        }

        public static void Unregister(ActorWorld world)
        {
            for (var i = Worlds.Count - 1; i >= 0; i--)
            {
                if (!Worlds[i].TryGetTarget(out var candidate) || ReferenceEquals(candidate, world))
                    Worlds.RemoveAt(i);
            }
        }

        public static void GetWorlds(List<ActorWorld> result)
        {
            result.Clear();

            for (var i = Worlds.Count - 1; i >= 0; i--)
            {
                if (!Worlds[i].TryGetTarget(out var world) || world.IsDisposed)
                {
                    Worlds.RemoveAt(i);
                    continue;
                }

                result.Add(world);
            }
        }

        private static void Prune()
        {
            for (var i = Worlds.Count - 1; i >= 0; i--)
            {
                if (!Worlds[i].TryGetTarget(out var world) || world.IsDisposed)
                    Worlds.RemoveAt(i);
            }
        }
    }
}
#endif
